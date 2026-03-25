using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class AttendanceServiceTests
{
    [Theory]
    [AutoMoqData]
    public async Task CreateQrSession_MissingClient_Throws(
        [Frozen] IClientRepository clientRepo,
        AttendanceService attendanceService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Client?)null);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => attendanceService.CreateQrSessionAsync(userId));
        Assert.Equal("Client profile not found.", exception.Message);
        clientRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CreateQrSession_CreatesWithDefaultTtl(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IQrSessionRepository qrRepo,
        AttendanceService attendanceService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        var qrSessionRepositoryMock = Mock.Get(qrRepo);

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

        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);

        QrSession? createdSession = null;
        qrSessionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<QrSession>()))
            .Callback<QrSession>(qrSession => createdSession = qrSession)
            .Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var result = await attendanceService.CreateQrSessionAsync(userId);
        var after = DateTime.UtcNow;

        Assert.NotNull(createdSession);
        Assert.Equal(client.Id, createdSession!.ClientId);
        Assert.NotEqual(Guid.Empty, createdSession.Token);
        Assert.True(createdSession.ExpireDate > createdSession.CreateDate);

        Assert.Equal(createdSession.Token, result.Token);
        Assert.Equal(createdSession.ExpireDate, result.ExpireDate);

        var expectedMin = before.AddMinutes(3).AddSeconds(-10);
        var expectedMax = after.AddMinutes(3).AddSeconds(10);
        Assert.True(result.ExpireDate >= expectedMin && result.ExpireDate <= expectedMax);

        clientRepositoryMock.VerifyAll();
        qrSessionRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CheckIn_MissingSession_Throws(
        [Frozen] IQrSessionRepository qrRepo,
        AttendanceService attendanceService
    )
    {
        var token = Guid.NewGuid();
        var qrSessionRepositoryMock = Mock.Get(qrRepo);
        qrSessionRepositoryMock.Setup(r => r.GetByTokenAsync(token)).ReturnsAsync((QrSession?)null);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => attendanceService.CheckInByTokenAsync(token));
        Assert.Equal("QR session not found.", exception.Message);
        qrSessionRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CheckIn_ExpiredSession_Throws(
        [Frozen] IQrSessionRepository qrRepo,
        AttendanceService attendanceService
    )
    {
        var token = Guid.NewGuid();
        var qrSessionRepositoryMock = Mock.Get(qrRepo);

        var expiredSession = new QrSession
        {
            Token = token,
            CreateDate = DateTime.UtcNow.AddMinutes(-10),
            ExpireDate = DateTime.UtcNow.AddMinutes(-1),
            ClientId = Guid.NewGuid()
        };

        qrSessionRepositoryMock.Setup(r => r.GetByTokenAsync(token)).ReturnsAsync(expiredSession);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => attendanceService.CheckInByTokenAsync(token));
        Assert.Equal("QR session expired.", exception.Message);
        qrSessionRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CheckIn_OpenVisit_ReturnsExisting(
        [Frozen] IQrSessionRepository qrRepo,
        [Frozen] IVisitLogRepository visitRepo,
        AttendanceService attendanceService
    )
    {
        var token = Guid.NewGuid();
        var qrSessionRepositoryMock = Mock.Get(qrRepo);
        var visitLogRepositoryMock = Mock.Get(visitRepo);

        var clientId = Guid.NewGuid();
        var qrSession = new QrSession
        {
            Token = token,
            CreateDate = DateTime.UtcNow,
            ExpireDate = DateTime.UtcNow.AddMinutes(5),
            ClientId = clientId
        };

        var openVisitLog = new VisitLog
        {
            Id = 10,
            ClientId = clientId,
            MembershipId = 99,
            EnterDate = DateTime.UtcNow.AddMinutes(-30),
            LeaveDate = null
        };

        qrSessionRepositoryMock.Setup(r => r.GetByTokenAsync(token)).ReturnsAsync(qrSession);

        visitLogRepositoryMock.Setup(r => r.GetOpenVisitByClientIdAsync(clientId)).ReturnsAsync(openVisitLog);

        var result = await attendanceService.CheckInByTokenAsync(token);
        Assert.Equal(10, result.Id);
        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(99, result.MembershipId);
        Assert.Null(result.LeaveDate);

        visitLogRepositoryMock.VerifyAll();
        qrSessionRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CheckIn_NoOpenVisit_CreatesVisitAndDeletesToken(
        [Frozen] IQrSessionRepository qrRepo,
        [Frozen] IVisitLogRepository visitRepo,
        [Frozen] IMembershipRepository membershipRepo,
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
            Id = 77,
            ClientId = clientId,
            PlanId = 1,
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

        var before = DateTime.UtcNow;
        var result = await attendanceService.CheckInByTokenAsync(token);
        var after = DateTime.UtcNow;

        Assert.NotNull(createdVisitLog);
        Assert.Equal(clientId, createdVisitLog!.ClientId);
        Assert.Equal(77, createdVisitLog.MembershipId);
        Assert.True(createdVisitLog.EnterDate >= before.AddSeconds(-5) &&
                    createdVisitLog.EnterDate <= after.AddSeconds(5));
        Assert.Null(createdVisitLog.LeaveDate);

        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(77, result.MembershipId);
        Assert.Null(result.LeaveDate);

        membershipRepositoryMock.VerifyAll();
        visitLogRepositoryMock.VerifyAll();
        qrSessionRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CheckOut_MissingOpenVisit_Throws(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IVisitLogRepository visitRepo,
        AttendanceService attendanceService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        var visitLogRepositoryMock = Mock.Get(visitRepo);

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

        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);

        visitLogRepositoryMock.Setup(r => r.GetOpenVisitByClientIdAsync(client.Id)).ReturnsAsync((VisitLog?)null);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => attendanceService.CheckOutAsync(userId));
        Assert.Equal("Open visit not found.", exception.Message);

        clientRepositoryMock.VerifyAll();
        visitLogRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CheckOut_SetsLeaveDate_Updates(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IVisitLogRepository visitRepo,
        AttendanceService attendanceService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        var visitLogRepositoryMock = Mock.Get(visitRepo);

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
            Id = 55,
            ClientId = client.Id,
            MembershipId = 77,
            EnterDate = DateTime.UtcNow.AddMinutes(-30),
            LeaveDate = null
        };

        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);

        visitLogRepositoryMock.Setup(r => r.GetOpenVisitByClientIdAsync(client.Id)).ReturnsAsync(openVisitLog);
        visitLogRepositoryMock.Setup(r => r.UpdateAsync(openVisitLog)).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var result = await attendanceService.CheckOutAsync(userId);
        var after = DateTime.UtcNow;

        Assert.NotNull(openVisitLog.LeaveDate);
        Assert.True(openVisitLog.LeaveDate >= before.AddSeconds(-5) && openVisitLog.LeaveDate <= after.AddSeconds(5));
        Assert.Equal(55, result.Id);
        Assert.NotNull(result.LeaveDate);

        clientRepositoryMock.VerifyAll();
        visitLogRepositoryMock.VerifyAll();
    }
}