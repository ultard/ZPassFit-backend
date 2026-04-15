using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class DashboardServiceTests
{
    [Theory]
    [AutoMoqData]
    public async Task GetEmployeeDashboard_Maps(
        [Frozen] IVisitLogRepository visitLogRepository,
        [Frozen] IQrSessionRepository qrSessionRepository,
        DashboardService dashboardService
    )
    {
        var utc = new DateTime(2026, 4, 15, 14, 30, 0, DateTimeKind.Utc);

        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+70000000000",
            Email = "i@example.com"
        };

        var visit = new VisitLog
        {
            Id = 5,
            ClientId = client.Id,
            EnterDate = utc,
            Client = client
        };

        var visitMock = Mock.Get(visitLogRepository);
        var qrMock = Mock.Get(qrSessionRepository);

        visitMock.Setup(r => r.CountVisitsEnteringBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(3);
        visitMock.Setup(r => r.CountOpenVisitsAsync()).ReturnsAsync(2);
        qrMock.Setup(r => r.CountActiveAsync(It.IsAny<DateTime>())).ReturnsAsync(4);
        visitMock.Setup(r => r.GetRecentVisitsWithClientAsync(20)).ReturnsAsync([visit]);

        var result = await dashboardService.GetEmployeeDashboardAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.Zero, result.TodayUtcStart.TimeOfDay);
        Assert.Equal(result.TodayUtcStart.AddDays(1), result.TodayUtcEnd);
        Assert.Equal(3, result.VisitsTodayCount);
        Assert.Equal(2, result.ClientsInClubNow);
        Assert.Equal(4, result.ActiveQrSessionsCount);
        Assert.Single(result.RecentCheckIns);
        Assert.Equal(5, result.RecentCheckIns[0].VisitId);
        Assert.Equal(client.Id, result.RecentCheckIns[0].ClientId);
        Assert.Equal("Иванов Иван Иванович", result.RecentCheckIns[0].ClientFullName);
        Assert.Equal(utc, result.RecentCheckIns[0].EnterDateUtc);

        visitMock.VerifyAll();
        qrMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetAdminDashboard_IncludesStaffAndTotals(
        [Frozen] IVisitLogRepository visitLogRepository,
        [Frozen] IQrSessionRepository qrSessionRepository,
        [Frozen] IClientRepository clientRepository,
        [Frozen] IMembershipRepository membershipRepository,
        [Frozen] IPaymentRepository paymentRepository,
        DashboardService dashboardService
    )
    {
        var visitMock = Mock.Get(visitLogRepository);
        var qrMock = Mock.Get(qrSessionRepository);
        var clientMock = Mock.Get(clientRepository);
        var membershipMock = Mock.Get(membershipRepository);
        var paymentMock = Mock.Get(paymentRepository);

        visitMock.Setup(r => r.CountVisitsEnteringBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(0);
        visitMock.Setup(r => r.CountOpenVisitsAsync()).ReturnsAsync(0);
        qrMock.Setup(r => r.CountActiveAsync(It.IsAny<DateTime>())).ReturnsAsync(0);
        visitMock.Setup(r => r.GetRecentVisitsWithClientAsync(20)).ReturnsAsync([]);

        clientMock.Setup(r => r.CountAsync()).ReturnsAsync(100);
        membershipMock.Setup(r => r.CountActiveAsync(It.IsAny<DateTime>())).ReturnsAsync(42);
        paymentMock.Setup(r => r.GetCompletedPaymentsSummaryBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((5, 15000L));

        var result = await dashboardService.GetAdminDashboardAsync(TestContext.Current.CancellationToken);

        Assert.Equal(100, result.TotalClients);
        Assert.Equal(42, result.ActiveMemberships);
        Assert.Equal(5, result.PaymentsTodayCount);
        Assert.Equal(15000L, result.PaymentsTodayTotalAmount);
        Assert.Equal(0, result.Staff.VisitsTodayCount);

        visitMock.VerifyAll();
        qrMock.VerifyAll();
        clientMock.VerifyAll();
        membershipMock.VerifyAll();
        paymentMock.VerifyAll();
    }
}
