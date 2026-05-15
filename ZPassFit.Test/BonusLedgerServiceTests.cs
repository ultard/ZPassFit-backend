using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZPassFit.Attendance;
using ZPassFit.Data;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class BonusLedgerServiceTests
{
    [Fact]
    public async Task TryAccrueDisciplineBonus_OnCheckout_IncreasesClientBonuses()
    {
        var options = Options.Create(
            new AttendanceBonusOptions
            {
                DisciplineBonusPoints = 15,
                BonusValidityDays = 30,
                MinVisitDurationMinutes = 0
            }
        );

        await using var db = CreateDb();
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u-bonus",
            LastName = "Test",
            FirstName = "User",
            MiddleName = "Middle",
            BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = ClientGender.Unknown,
            Phone = "+70000000001",
            Email = "bonus@test.local",
            Bonuses = 5
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var clientRepo = new ClientRepository(db);
        var bonusRepo = new BonusTransactionRepository(db);
        var sut = new BonusLedgerService(db, clientRepo, bonusRepo, options);

        var visit = new VisitLog
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            MembershipId = Guid.NewGuid(),
            EnterDate = DateTime.UtcNow.AddHours(-1),
            LeaveDate = DateTime.UtcNow
        };

        var accrued = await sut.TryAccrueDisciplineBonusAsync(client, visit, DateTime.UtcNow);

        Assert.Equal(15, accrued);
        var reloaded = await db.Clients.SingleAsync(c => c.Id == client.Id);
        Assert.Equal(20, reloaded.Bonuses);
    }

    [Fact]
    public async Task BurnExpiredBonuses_ReducesBalanceAndCreatesExpireTransaction()
    {
        var options = Options.Create(new AttendanceBonusOptions());

        await using var db = CreateDb();
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u-burn",
            LastName = "Burn",
            FirstName = "Test",
            MiddleName = "M",
            BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = ClientGender.Unknown,
            Phone = "+70000000002",
            Email = "burn@test.local",
            Bonuses = 40
        };
        var accrualId = Guid.NewGuid();
        db.Clients.Add(client);
        db.BonusTransactions.Add(
            new BonusTransaction
            {
                Id = accrualId,
                ClientId = client.Id,
                Type = BonusTransactionType.Accrual,
                Amount = 40,
                CreateDate = DateTime.UtcNow.AddDays(-100),
                ExpireDate = DateTime.UtcNow.AddDays(-1)
            }
        );
        await db.SaveChangesAsync();

        var clientRepo = new ClientRepository(db);
        var bonusRepo = new BonusTransactionRepository(db);
        var sut = new BonusLedgerService(db, clientRepo, bonusRepo, options);

        var burned = await sut.BurnExpiredBonusesAsync(client.Id, DateTime.UtcNow);

        Assert.Equal(40, burned);
        var reloaded = await db.Clients.SingleAsync(c => c.Id == client.Id);
        Assert.Equal(0, reloaded.Bonuses);
        Assert.True(
            await db.BonusTransactions.AnyAsync(t =>
                t.Type == BonusTransactionType.Expire && t.RelatedTransactionId == accrualId)
        );
    }

    private static ApplicationDbContext CreateDb()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(dbOptions);
    }
}
