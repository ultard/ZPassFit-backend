using Microsoft.Extensions.Options;
using ZPassFit.Data.Repositories.Attendance;

namespace ZPassFit.Workers;

/// <summary>
/// Периодически закрывает открытые посещения, если с момента входа прошло больше <see cref="StaleOpenVisitsWorkerOptions.MaxOpenDuration"/>.
/// </summary>
public class StaleOpenVisitsWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<StaleOpenVisitsWorkerOptions> options,
    ILogger<StaleOpenVisitsWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        if (opts.CheckInterval <= TimeSpan.Zero || opts.MaxOpenDuration <= TimeSpan.Zero)
        {
            logger.LogWarning(
                "StaleOpenVisits worker disabled: invalid CheckInterval or MaxOpenDuration in configuration.");
            return;
        }

        using var timer = new PeriodicTimer(opts.CheckInterval);

        await RunOnceAsync(opts.MaxOpenDuration, stoppingToken);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(opts.MaxOpenDuration, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
    }

    private async Task RunOnceAsync(TimeSpan maxOpenDuration, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var visitLogs = scope.ServiceProvider.GetRequiredService<IVisitLogRepository>();
            var closed = await visitLogs.AutoCloseStaleOpenVisitsAsync(maxOpenDuration, cancellationToken);
            if (closed > 0)
                logger.LogInformation("Auto-closed {Count} stale open visit(s).", closed);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to auto-close stale open visits.");
        }
    }
}
