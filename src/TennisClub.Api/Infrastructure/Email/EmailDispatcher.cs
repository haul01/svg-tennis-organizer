namespace TennisClub.Api.Infrastructure.Email;

/// <summary>
/// Hosted service that drains the EmailQueue and hands each message
/// to the configured IEmailSender. Sender resolution happens per-message
/// from a fresh DI scope, matching the request lifetime model.
/// </summary>
public sealed class EmailDispatcher(
    EmailQueue queue,
    IServiceProvider services,
    ILogger<EmailDispatcher> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.ReadAllAsync(stoppingToken))
        {
            using var scope = services.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            try
            {
                await sender.SendAsync(message, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Persistence-backed retry is a Phase-9 concern; for MVP we
                // log and move on so a single transient SMTP failure doesn't
                // stall the queue.
                log.LogError(ex, "Failed to dispatch email to {To}", message.To);
            }
        }
    }
}
