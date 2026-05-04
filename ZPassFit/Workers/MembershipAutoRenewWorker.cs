using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZPassFit.Data;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Workers;

/// <summary>
/// Автоматически продлевает абонементы, списывая деньги с баланса клиента.
/// После вызова /Membership/cancel автопродление выключается (период остаётся действительным до ExpireDate).
/// </summary>
public class MembershipAutoRenewWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MembershipAutoRenewWorkerOptions> options,
    ILogger<MembershipAutoRenewWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        if (opts.CheckInterval <= TimeSpan.Zero || opts.MaxRenewalsPerMembership < 1)
        {
            logger.LogWarning(
                "MembershipAutoRenew worker disabled: invalid CheckInterval or MaxRenewalsPerMembership.");
            return;
        }

        using var timer = new PeriodicTimer(opts.CheckInterval);

        await RunOnceAsync(opts.MaxRenewalsPerMembership, stoppingToken);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(opts.MaxRenewalsPerMembership, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
    }

    private async Task RunOnceAsync(int maxRenewals, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;
            var due = await db.Memberships
                .Include(m => m.Plan)
                .Include(m => m.Client)
                .Where(m =>
                    m.Status == MembershipStatus.Active &&
                    m.AutoRenewEnabled &&
                    m.ExpireDate <= now)
                .ToListAsync(cancellationToken);

            foreach (var membership in due)
            {
                var periodDays = Math.Max(
                    1,
                    (int)(membership.ExpireDate.Date - membership.ActivatedDate.Date).TotalDays
                );

                var renewals = 0;
                while (membership.ExpireDate <= now && renewals < maxRenewals)
                {
                    if (membership.Client.Balance < membership.Plan.Price)
                    {
                        membership.Status = MembershipStatus.Frozen;
                        membership.AutoRenewEnabled = false;
                        break;
                    }

                    membership.Client.Balance -= membership.Plan.Price;

                    db.Payments.Add(new Payment
                    {
                        Amount = membership.Plan.Price,
                        Method = PaymentMethod.Balance,
                        Status = PaymentStatus.Completed,
                        PaymentDate = now,
                        ClientId = membership.ClientId,
                        EmployeeId = null
                    });

                    var oldExpire = membership.ExpireDate;
                    membership.ActivatedDate = oldExpire;
                    membership.ExpireDate = oldExpire.AddDays(periodDays);
                    membership.Status = MembershipStatus.Active;

                    renewals++;
                }
            }

            // 2) Если автопродление выключено и срок истёк — переводим статус в Expired.
            var expiredWithoutAutoRenew = await db.Memberships
                .Where(m =>
                    m.Status == MembershipStatus.Active &&
                    !m.AutoRenewEnabled &&
                    m.ExpireDate <= now)
                .ToListAsync(cancellationToken);

            foreach (var membership in expiredWithoutAutoRenew)
                membership.Status = MembershipStatus.Expired;

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to auto-renew memberships.");
        }
    }
}

