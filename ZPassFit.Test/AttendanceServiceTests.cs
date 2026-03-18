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
    [Fact]
    public async Task CreateQrSessionAsync_WhenClientMissing_Throws()
    {
        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        clientRepo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync((Client?)null);

        var svc = new AttendanceService(
            clientRepository: clientRepo.Object,
            membershipRepository: Mock.Of<IMembershipRepository>(),
            qrSessionRepository: Mock.Of<IQrSessionRepository>(),
            visitLogRepository: Mock.Of<IVisitLogRepository>()
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateQrSessionAsync("u1"));
        Assert.Equal("Client profile not found.", ex.Message);
        clientRepo.VerifyAll();
    }

    [Fact]
    public async Task CreateQrSessionAsync_CreatesSession_WithDefaultTtl()
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "0",
            Email = "a@a.a"
        };

        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        clientRepo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(client);

        var qrRepo = new Mock<IQrSessionRepository>(MockBehavior.Strict);
        QrSession? added = null;
        qrRepo.Setup(r => r.AddAsync(It.IsAny<QrSession>()))
            .Callback<QrSession>(s => added = s)
            .Returns(Task.CompletedTask);

        var svc = new AttendanceService(
            clientRepository: clientRepo.Object,
            membershipRepository: Mock.Of<IMembershipRepository>(),
            qrSessionRepository: qrRepo.Object,
            visitLogRepository: Mock.Of<IVisitLogRepository>()
        );

        var before = DateTime.UtcNow;
        var res = await svc.CreateQrSessionAsync("u1");
        var after = DateTime.UtcNow;

        Assert.NotNull(added);
        Assert.Equal(client.Id, added!.ClientId);
        Assert.False(string.IsNullOrWhiteSpace(added.Token));
        Assert.True(added.ExpireDate > added.CreateDate);

        Assert.Equal(added.Token, res.Token);
        Assert.Equal(added.ExpireDate, res.ExpireDate);

        // default ttl = 3 minutes, allow some jitter
        var expectedMin = before.AddMinutes(3).AddSeconds(-10);
        var expectedMax = after.AddMinutes(3).AddSeconds(10);
        Assert.True(res.ExpireDate >= expectedMin && res.ExpireDate <= expectedMax);

        clientRepo.VerifyAll();
        qrRepo.VerifyAll();
    }

    [Fact]
    public async Task CheckInByTokenAsync_WhenSessionMissing_Throws()
    {
        var qrRepo = new Mock<IQrSessionRepository>(MockBehavior.Strict);
        qrRepo.Setup(r => r.GetByTokenAsync("t")).ReturnsAsync((QrSession?)null);

        var svc = new AttendanceService(
            clientRepository: Mock.Of<IClientRepository>(),
            membershipRepository: Mock.Of<IMembershipRepository>(),
            qrSessionRepository: qrRepo.Object,
            visitLogRepository: Mock.Of<IVisitLogRepository>()
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CheckInByTokenAsync("t"));
        Assert.Equal("QR session not found.", ex.Message);
        qrRepo.VerifyAll();
    }

    [Fact]
    public async Task CheckInByTokenAsync_WhenSessionExpired_Throws()
    {
        var session = new QrSession
        {
            Token = "t",
            CreateDate = DateTime.UtcNow.AddMinutes(-10),
            ExpireDate = DateTime.UtcNow.AddMinutes(-1),
            ClientId = Guid.NewGuid()
        };

        var qrRepo = new Mock<IQrSessionRepository>(MockBehavior.Strict);
        qrRepo.Setup(r => r.GetByTokenAsync("t")).ReturnsAsync(session);

        var svc = new AttendanceService(
            clientRepository: Mock.Of<IClientRepository>(),
            membershipRepository: Mock.Of<IMembershipRepository>(),
            qrSessionRepository: qrRepo.Object,
            visitLogRepository: Mock.Of<IVisitLogRepository>()
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CheckInByTokenAsync("t"));
        Assert.Equal("QR session expired.", ex.Message);
        qrRepo.VerifyAll();
    }

    [Fact]
    public async Task CheckInByTokenAsync_WhenOpenVisitExists_ReturnsIt_WithoutCreatingNew()
    {
        var clientId = Guid.NewGuid();
        var session = new QrSession
        {
            Token = "t",
            CreateDate = DateTime.UtcNow,
            ExpireDate = DateTime.UtcNow.AddMinutes(5),
            ClientId = clientId
        };

        var open = new VisitLog
        {
            Id = 10,
            ClientId = clientId,
            MembershipId = 99,
            EnterDate = DateTime.UtcNow.AddMinutes(-30),
            LeaveDate = null
        };

        var qrRepo = new Mock<IQrSessionRepository>(MockBehavior.Strict);
        qrRepo.Setup(r => r.GetByTokenAsync("t")).ReturnsAsync(session);

        var visitRepo = new Mock<IVisitLogRepository>(MockBehavior.Strict);
        visitRepo.Setup(r => r.GetOpenVisitByClientIdAsync(clientId)).ReturnsAsync(open);

        var svc = new AttendanceService(
            clientRepository: Mock.Of<IClientRepository>(),
            membershipRepository: Mock.Of<IMembershipRepository>(),
            qrSessionRepository: qrRepo.Object,
            visitLogRepository: visitRepo.Object
        );

        var res = await svc.CheckInByTokenAsync("t");
        Assert.Equal(10, res.Id);
        Assert.Equal(clientId, res.ClientId);
        Assert.Equal(99, res.MembershipId);
        Assert.Null(res.LeaveDate);

        visitRepo.VerifyAll();
        qrRepo.VerifyAll();
    }

    [Fact]
    public async Task CheckInByTokenAsync_WhenNoOpenVisit_CreatesVisit_AndDeletesToken()
    {
        var clientId = Guid.NewGuid();
        var session = new QrSession
        {
            Token = "t",
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

        var qrRepo = new Mock<IQrSessionRepository>(MockBehavior.Strict);
        qrRepo.Setup(r => r.GetByTokenAsync("t")).ReturnsAsync(session);
        qrRepo.Setup(r => r.DeleteByTokenAsync("t")).Returns(Task.CompletedTask);

        var visitRepo = new Mock<IVisitLogRepository>(MockBehavior.Strict);
        visitRepo.Setup(r => r.GetOpenVisitByClientIdAsync(clientId)).ReturnsAsync((VisitLog?)null);

        VisitLog? added = null;
        visitRepo.Setup(r => r.AddAsync(It.IsAny<VisitLog>()))
            .Callback<VisitLog>(v => added = v)
            .Returns(Task.CompletedTask);

        var membershipRepo = new Mock<IMembershipRepository>(MockBehavior.Strict);
        membershipRepo.Setup(r => r.GetByClientIdAsync(clientId)).ReturnsAsync(membership);

        var svc = new AttendanceService(
            clientRepository: Mock.Of<IClientRepository>(),
            membershipRepository: membershipRepo.Object,
            qrSessionRepository: qrRepo.Object,
            visitLogRepository: visitRepo.Object
        );

        var before = DateTime.UtcNow;
        var res = await svc.CheckInByTokenAsync("t");
        var after = DateTime.UtcNow;

        Assert.NotNull(added);
        Assert.Equal(clientId, added!.ClientId);
        Assert.Equal(77, added.MembershipId);
        Assert.True(added.EnterDate >= before.AddSeconds(-5) && added.EnterDate <= after.AddSeconds(5));
        Assert.Null(added.LeaveDate);

        Assert.Equal(clientId, res.ClientId);
        Assert.Equal(77, res.MembershipId);
        Assert.Null(res.LeaveDate);

        membershipRepo.VerifyAll();
        visitRepo.VerifyAll();
        qrRepo.VerifyAll();
    }

    [Fact]
    public async Task CheckOutAsync_WhenOpenVisitMissing_Throws()
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "0",
            Email = "a@a.a"
        };

        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        clientRepo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(client);

        var visitRepo = new Mock<IVisitLogRepository>(MockBehavior.Strict);
        visitRepo.Setup(r => r.GetOpenVisitByClientIdAsync(client.Id)).ReturnsAsync((VisitLog?)null);

        var svc = new AttendanceService(
            clientRepository: clientRepo.Object,
            membershipRepository: Mock.Of<IMembershipRepository>(),
            qrSessionRepository: Mock.Of<IQrSessionRepository>(),
            visitLogRepository: visitRepo.Object
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CheckOutAsync("u1"));
        Assert.Equal("Open visit not found.", ex.Message);

        clientRepo.VerifyAll();
        visitRepo.VerifyAll();
    }

    [Fact]
    public async Task CheckOutAsync_SetsLeaveDate_AndUpdates()
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "0",
            Email = "a@a.a"
        };

        var open = new VisitLog
        {
            Id = 55,
            ClientId = client.Id,
            MembershipId = 77,
            EnterDate = DateTime.UtcNow.AddMinutes(-30),
            LeaveDate = null
        };

        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        clientRepo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(client);

        var visitRepo = new Mock<IVisitLogRepository>(MockBehavior.Strict);
        visitRepo.Setup(r => r.GetOpenVisitByClientIdAsync(client.Id)).ReturnsAsync(open);
        visitRepo.Setup(r => r.UpdateAsync(open)).Returns(Task.CompletedTask);

        var svc = new AttendanceService(
            clientRepository: clientRepo.Object,
            membershipRepository: Mock.Of<IMembershipRepository>(),
            qrSessionRepository: Mock.Of<IQrSessionRepository>(),
            visitLogRepository: visitRepo.Object
        );

        var before = DateTime.UtcNow;
        var res = await svc.CheckOutAsync("u1");
        var after = DateTime.UtcNow;

        Assert.NotNull(open.LeaveDate);
        Assert.True(open.LeaveDate >= before.AddSeconds(-5) && open.LeaveDate <= after.AddSeconds(5));
        Assert.Equal(55, res.Id);
        Assert.NotNull(res.LeaveDate);

        clientRepo.VerifyAll();
        visitRepo.VerifyAll();
    }
}

