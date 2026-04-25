using Microsoft.Extensions.Options;
using ZPassFit.Data.Repositories.Clients;

namespace ZPassFit.Workers;

/// <summary>
/// Периодически проверяет уровни клиентов и сбрасывает те, у кого истёк GraceDays с последнего посещения.
/// </summary>
public class ExpiredGraceLevelsWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ExpiredGraceLevelsWorkerOptions> options,
    ILogger<ExpiredGraceLevelsWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        if (opts.CheckInterval <= TimeSpan.Zero)
        {
            logger.LogWarning(
                "ExpiredGraceLevels worker disabled: invalid CheckInterval in configuration.");
            return;
        }

        using var timer = new PeriodicTimer(opts.CheckInterval);

        await RunOnceAsync(stoppingToken);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IClientLevelRepository>();

            var reset = await repo.ResetLevelsWithExpiredGraceAsync(cancellationToken);
            if (reset > 0)
                logger.LogInformation("Reset {Count} client level(s) due to expired grace.", reset);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to reset client levels with expired grace.");
        }
    }
}

