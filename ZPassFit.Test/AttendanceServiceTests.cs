using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Services.Implementations;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Test;

public class AttendanceServiceTests
{
    [Theory]
    [AutoMoqData]
    public async Task CheckIn_NoOpenVisit_CreatesVisitAndDeletesToken(
        [Frozen] IQrSessionRepository qrRepo,
        [Frozen] IVisitLogRepository visitRepo,
        [Frozen] IMembershipRepository membershipRepo,
        [Frozen] IBonusLedgerService bonusLedger,
        AttendanceService attendanceService
    )
    {
        var token = Guid.NewGuid();
        var qrSessionRepositoryMock = Mock.Get(qrRepo);
        var visitLogRepositoryMock = Mock.Get(visitRepo);
        var membershipRepositoryMock = Mock.Get(membershipRepo);

        var clientId = Guid.NewGuid();
        var qrSession = new QrSession
        {
            Token = token,
            CreateDate = DateTime.UtcNow,
            ExpireDate = DateTime.UtcNow.AddMinutes(5),
            ClientId = clientId
        };

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            PlanId = Guid.NewGuid(),
            Status = MembershipStatus.Active,
            ActivatedDate = DateTime.UtcNow.AddDays(-1),
            ExpireDate = DateTime.UtcNow.AddDays(29)
        };

        qrSessionRepositoryMock.Setup(r => r.GetByTokenAsync(token)).ReturnsAsync(qrSession);
        qrSessionRepositoryMock.Setup(r => r.DeleteByTokenAsync(token)).Returns(Task.CompletedTask);

        visitLogRepositoryMock.Setup(r => r.GetOpenVisitByClientIdAsync(clientId)).ReturnsAsync((VisitLog?)null);

        VisitLog? createdVisitLog = null;
        visitLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<VisitLog>()))
            .Callback<VisitLog>(visitLog => createdVisitLog = visitLog)
            .Returns(Task.CompletedTask);

        membershipRepositoryMock.Setup(r => r.GetByClientIdAsync(clientId)).ReturnsAsync(membership);

        Mock.Get(bonusLedger)
            .Setup(b => b.BurnExpiredBonusesAsync(clientId, It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        var before = DateTime.UtcNow;
        var result = await attendanceService.CheckInByTokenAsync(token);
        var after = DateTime.UtcNow;

        Assert.NotNull(createdVisitLog);
        Assert.Equal(clientId, createdVisitLog!.ClientId);
        Assert.Equal(membership.Id, createdVisitLog.MembershipId);
        Assert.True(createdVisitLog.EnterDate >= before.AddSeconds(-5) &&
                    createdVisitLog.EnterDate <= after.AddSeconds(5));
        Assert.Null(createdVisitLog.LeaveDate);

        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(membership.Id, result.MembershipId);
        Assert.Null(result.LeaveDate);

        membershipRepositoryMock.VerifyAll();
        visitLogRepositoryMock.VerifyAll();
        qrSessionRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CheckOut_SetsLeaveDate_Updates(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IVisitLogRepository visitRepo,
        [Frozen] IBonusLedgerService bonusLedger,
        AttendanceService attendanceService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        var visitLogRepositoryMock = Mock.Get(visitRepo);

        var visitId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LastName = "Ivanov",
            FirstName = "Ivan",
            MiddleName = "Ivanovich",
            BirthDate = new DateTime(2000, 1, 2),
            Gender = ClientGender.Male,
            Phone = "+70000000000",
            Email = "ivan@example.com"
        };

        var openVisitLog = new VisitLog
        {
            Id = visitId,
            ClientId = client.Id,
            MembershipId = membershipId,
            EnterDate = DateTime.UtcNow.AddMinutes(-30),
            LeaveDate = null
        };

        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);

        visitLogRepositoryMock.Setup(r => r.GetOpenVisitByClientIdAsync(client.Id)).ReturnsAsync(openVisitLog);
        visitLogRepositoryMock.Setup(r => r.UpdateAsync(openVisitLog)).Returns(Task.CompletedTask);

        Mock.Get(bonusLedger)
            .Setup(b => b.BurnExpiredBonusesAsync(client.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(0);
        Mock.Get(bonusLedger)
            .Setup(b => b.TryAccrueDisciplineBonusAsync(client, openVisitLog, It.IsAny<DateTime>()))
            .ReturnsAsync(10);

        var before = DateTime.UtcNow;
        var result = await attendanceService.CheckOutAsync(userId);
        var after = DateTime.UtcNow;

        Assert.NotNull(openVisitLog.LeaveDate);
        Assert.True(openVisitLog.LeaveDate >= before.AddSeconds(-5) && openVisitLog.LeaveDate <= after.AddSeconds(5));
        Assert.Equal(visitId, result.Id);
        Assert.NotNull(result.LeaveDate);
        Assert.Equal(10, result.DisciplineBonusAccrued);

        clientRepositoryMock.VerifyAll();
        visitLogRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CheckIn_ExpiredMembership_Throws(
        [Frozen] IQrSessionRepository qrRepo,
        [Frozen] IVisitLogRepository visitRepo,
        [Frozen] IMembershipRepository membershipRepo,
        AttendanceService attendanceService
    )
    {
        var token = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var qrSession = new QrSession
        {
            Token = token,
            CreateDate = DateTime.UtcNow,
            ExpireDate = DateTime.UtcNow.AddMinutes(5),
            ClientId = clientId
        };

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            PlanId = Guid.NewGuid(),
            Status = MembershipStatus.Active,
            ActivatedDate = DateTime.UtcNow.AddDays(-60),
            ExpireDate = DateTime.UtcNow.AddDays(-1)
        };

        Mock.Get(qrRepo).Setup(r => r.GetByTokenAsync(token)).ReturnsAsync(qrSession);
        Mock.Get(visitRepo).Setup(r => r.GetOpenVisitByClientIdAsync(clientId)).ReturnsAsync((VisitLog?)null);
        Mock.Get(membershipRepo).Setup(r => r.GetByClientIdAsync(clientId)).ReturnsAsync(membership);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => attendanceService.CheckInByTokenAsync(token));
        Assert.Equal("Membership has expired.", exception.Message);
    }
}