using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PayFlow.Worker.Workers;

/// <summary>
/// Background worker that consumes notification events from RabbitMQ
/// and processes them (e.g., sending emails, push notifications, webhooks).
/// </summary>
public class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(ILogger<NotificationWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // In a full implementation, this would consume from RabbitMQ
                // queue "payflow.notifications" and dispatch to notification channels.
                //
                // Example flow:
                // 1. Consume message from queue
                // 2. Deserialize event
                // 3. Route to appropriate handler:
                //    - payment.completed → send receipt email + push notification
                //    - payment.failed → send failure alert
                //    - refund.processed → send refund confirmation
                // 4. Acknowledge message
                
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification worker");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Notification worker stopped");
    }
}
