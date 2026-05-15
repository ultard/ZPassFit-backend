using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZPassFit.Attendance;
using ZPassFit.Data;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class BonusLedgerService(
    ApplicationDbContext db,
    IClientRepository clientRepository,
    IBonusTransactionRepository bonusTransactionRepository,
    IOptions<AttendanceBonusOptions> bonusOptions
) : IBonusLedgerService
{
    public async Task<int> BurnExpiredBonusesAsync(Guid clientId, DateTime utcNow)
    {
        var client = await db.Clients.FindAsync(clientId);
        if (client is null)
            return 0;

        var expiredAccruals = await db.BonusTransactions
            .Where(t =>
                t.ClientId == clientId
                && t.Type == BonusTransactionType.Accrual
                && t.ExpireDate != null
                && t.ExpireDate < utcNow
                && t.Amount > 0)
            .ToListAsync();

        if (expiredAccruals.Count == 0)
            return 0;

        var accrualIds = expiredAccruals.Select(t => t.Id).ToList();
        var alreadyBurnedIds = await db.BonusTransactions
            .Where(t =>
                t.Type == BonusTransactionType.Expire
                && t.RelatedTransactionId != null
                && accrualIds.Contains(t.RelatedTransactionId.Value))
            .Select(t => t.RelatedTransactionId!.Value)
            .ToListAsync();

        var burnedSet = alreadyBurnedIds.ToHashSet();

        var toBurn = expiredAccruals
            .Where(a => !burnedSet.Contains(a.Id))
            .ToList();

        if (toBurn.Count == 0)
            return 0;

        var totalBurn = 0;
        foreach (var accrual in toBurn)
        {
            var burnAmount = Math.Min(accrual.Amount, client.Bonuses);
            if (burnAmount > 0)
            {
                client.Bonuses -= burnAmount;
                totalBurn += burnAmount;
            }

            await bonusTransactionRepository.AddAsync(
                new BonusTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    Type = BonusTransactionType.Expire,
                    Amount = accrual.Amount,
                    CreateDate = utcNow,
                    RelatedTransactionId = accrual.Id
                }
            );
        }

        if (totalBurn > 0)
            await clientRepository.UpdateAsync(client);

        return totalBurn;
    }

    public async Task<int> TryAccrueDisciplineBonusAsync(Client client, VisitLog visit, DateTime utcNow)
    {
        var options = bonusOptions.Value;
        if (options.DisciplineBonusPoints <= 0)
            return 0;

        if (visit.LeaveDate is null)
            return 0;

        var alreadyAccrued = await db.BonusTransactions.AnyAsync(t =>
            t.ClientId == client.Id
            && t.VisitLogId == visit.Id
            && t.Type == BonusTransactionType.Accrual);

        if (alreadyAccrued)
            return 0;

        var duration = visit.LeaveDate.Value - visit.EnterDate;
        if (duration < TimeSpan.FromMinutes(options.MinVisitDurationMinutes))
            return 0;

        var points = options.DisciplineBonusPoints;
        var expireDate = options.BonusValidityDays > 0
            ? utcNow.Date.AddDays(options.BonusValidityDays)
            : (DateTime?)null;

        client.Bonuses += points;
        await clientRepository.UpdateAsync(client);

        await bonusTransactionRepository.AddAsync(
            new BonusTransaction
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = BonusTransactionType.Accrual,
                Amount = points,
                CreateDate = utcNow,
                ExpireDate = expireDate,
                VisitLogId = visit.Id
            }
        );

        return points;
    }
}
