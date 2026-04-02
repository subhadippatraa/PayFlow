using PayFlow.Application.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PayFlow.Infrastructure.Messaging;

public class RabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _initialized;
    private readonly object _lock = new();
    private const string ExchangeName = "payflow.events";

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private void EnsureInitialized()
    {
        if (_initialized && _channel is { IsOpen: true })
            return;

        lock (_lock)
        {
            if (_initialized && _channel is { IsOpen: true })
                return;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMq:Host"] ?? "localhost",
                    Port = int.TryParse(_configuration["RabbitMq:Port"], out var port) ? port : 5672,
                    UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                    Password = _configuration["RabbitMq:Password"] ?? "guest",
                    VirtualHost = _configuration["RabbitMq:VirtualHost"] ?? "/",
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare topic exchange
                _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

                // Dead-letter exchange
                _channel.ExchangeDeclare("payflow.dlx", ExchangeType.Fanout, durable: true, autoDelete: false);
                _channel.QueueDeclare("payflow.dead-letters", durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind("payflow.dead-letters", "payflow.dlx", string.Empty);

                // Consumer queues
                var queueArgs = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", "payflow.dlx" },
                    { "x-message-ttl", 86400000 }
                };

                DeclareAndBind("payflow.notifications", queueArgs, "payment.completed", "payment.failed", "refund.processed");
                DeclareAndBind("payflow.reconciliation", queueArgs, "payment.completed", "refund.processed");

                _initialized = true;
                _logger.LogInformation("RabbitMQ publisher initialized with exchange {Exchange}", ExchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize RabbitMQ. Events will be logged but not published.");
            }
        }
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            EnsureInitialized();

            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogWarning("RabbitMQ channel not available. Event {EventType} will not be published.", typeof(T).Name);
                return Task.CompletedTask;
            }

            var routingKey = GetRoutingKey(@event);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(ExchangeName, routingKey, properties, body);

            _logger.LogInformation("Published event {EventType} with routing key {RoutingKey}", typeof(T).Name, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to RabbitMQ", typeof(T).Name);
            // Graceful degradation — the outbox publisher will retry
        }

        return Task.CompletedTask;
    }

    private void DeclareAndBind(string queueName, Dictionary<string, object> args, params string[] routingKeys)
    {
        _channel!.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        foreach (var key in routingKeys)
            _channel.QueueBind(queueName, ExchangeName, key);
    }

    private static string GetRoutingKey<T>(T @event)
    {
        return typeof(T).Name switch
        {
            "PaymentCompletedEvent" => "payment.completed",
            "PaymentFailedEvent" => "payment.failed",
            "RefundProcessedEvent" => "refund.processed",
            "WalletTopUpEvent" => "wallet.topup",
            _ => "event.unknown"
        };
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
