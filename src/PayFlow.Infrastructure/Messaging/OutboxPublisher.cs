using PayFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PayFlow.Infrastructure.Messaging;

/// <summary>
/// Background service that polls the OutboxMessages table and publishes
/// unpublished events to RabbitMQ. This guarantees at-least-once delivery
/// even if RabbitMQ was temporarily unavailable when the event was created.
/// </summary>
public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private IConnection? _connection;
    private IModel? _channel;
    private readonly IConfiguration _configuration;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisher> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PayFlowDbContext>();

        var messages = await context.OutboxMessages
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
            return;

        EnsureChannel();

        foreach (var message in messages)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message.Payload);
                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.CorrelationId = message.CorrelationId ?? string.Empty;

                _channel.BasicPublish(
                    exchange: "payflow.events",
                    routingKey: message.EventType,
                    basicProperties: properties,
                    body: body);

                message.MarkPublished();

                _logger.LogDebug("Outbox message {Id} published with type {EventType}", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish outbox message {Id}", message.Id);
                break; // Stop processing to avoid out-of-order delivery
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true })
            return;

        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"] ?? "localhost",
            Port = int.TryParse(_configuration["RabbitMq:Port"], out var port) ? port : 5672,
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest"
        };

        _connection?.Dispose();
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
