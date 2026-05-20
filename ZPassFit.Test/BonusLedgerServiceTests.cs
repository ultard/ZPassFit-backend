using AutoFixture.Xunit3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using ZPassFit.Data;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Options.Attendance;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class BonusLedgerServiceTests
{
    [Theory]
    [AutoMoqData]
    public async Task TryAccrueDisciplineBonus_OnCheckout_IncreasesClientBonuses(
        [Frozen] Mock<IClientRepository> clientRepo,
        [Frozen] Mock<IBonusTransactionRepository> bonusRepo,
        Mock<IOptions<AttendanceBonusOptions>> bonusOptionsMock
    )
    {
        bonusOptionsMock
            .Setup(o => o.Value)
            .Returns(
                new AttendanceBonusOptions
                {
                    DisciplineBonusPoints = 15,
                    BonusValidityDays = 30,
                    MinVisitDurationMinutes = 0
                }
            );

        clientRepo
            .Setup(r => r.UpdateAsync(It.Is<Client>(c => c.Bonuses == 20)))
            .Returns(Task.CompletedTask);
        bonusRepo.Setup(r => r.AddAsync(It.IsAny<BonusTransaction>())).Returns(Task.CompletedTask);

        await using var db = CreateDb();
        var sut = new BonusLedgerService(db, clientRepo.Object, bonusRepo.Object, bonusOptionsMock.Object);

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
        Assert.Equal(20, client.Bonuses);
        clientRepo.Verify(r => r.UpdateAsync(It.Is<Client>(c => c.Bonuses == 20)), Times.Once);
        bonusRepo.Verify(
            r =>
                r.AddAsync(
                    It.Is<BonusTransaction>(t =>
                        t.ClientId == client.Id
                        && t.Type == BonusTransactionType.Accrual
                        && t.Amount == 15
                        && t.VisitLogId == visit.Id
                    )
                ),
            Times.Once
        );
    }

    [Theory]
    [AutoMoqData]
    public async Task BurnExpiredBonuses_ReducesBalanceAndCreatesExpireTransaction(
        [Frozen] Mock<IClientRepository> clientRepo,
        [Frozen] Mock<IBonusTransactionRepository> bonusRepo,
        Mock<IOptions<AttendanceBonusOptions>> bonusOptionsMock
    )
    {
        bonusOptionsMock.Setup(o => o.Value).Returns(new AttendanceBonusOptions());

        clientRepo.Setup(r => r.UpdateAsync(It.IsAny<Client>())).Returns(Task.CompletedTask);
        bonusRepo.Setup(r => r.AddAsync(It.IsAny<BonusTransaction>())).Returns(Task.CompletedTask);

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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new BonusLedgerService(db, clientRepo.Object, bonusRepo.Object, bonusOptionsMock.Object);

        var burned = await sut.BurnExpiredBonusesAsync(client.Id, DateTime.UtcNow);

        Assert.Equal(40, burned);
        Assert.Equal(0, client.Bonuses);
        clientRepo.Verify(r => r.UpdateAsync(It.Is<Client>(c => c.Id == client.Id && c.Bonuses == 0)), Times.Once);
        bonusRepo.Verify(
            r =>
                r.AddAsync(
                    It.Is<BonusTransaction>(t =>
                        t.Type == BonusTransactionType.Expire
                        && t.RelatedTransactionId == accrualId
                        && t.Amount == 40
                        && t.ClientId == client.Id
                    )
                ),
            Times.Once
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