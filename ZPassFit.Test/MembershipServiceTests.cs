using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class MembershipServiceTests
{
    [Theory]
    [AutoMoqData]
    public async Task GetPlans_Maps(
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var membershipPlanRepositoryMock = Mock.Get(planRepo);
        membershipPlanRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new MembershipPlan
            {
                Id = 1,
                Name = "Base",
                Description = "Standard access",
                Durations = [30, 90],
                Price = 1000
            }
        ]);

        var plans = (await membershipService.GetPlansAsync()).ToArray();
        Assert.Single(plans);
        Assert.Equal(1, plans[0].Id);
        Assert.Equal("Base", plans[0].Name);
        Assert.Equal("Standard access", plans[0].Description);
        Assert.Equal([30, 90], plans[0].Durations);
        Assert.Equal(1000, plans[0].Price);

        membershipPlanRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetMyMembership_MissingClient_ReturnsNull(
        [Frozen] IClientRepository clientRepo,
        MembershipService membershipService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Client?)null);

        var result = await membershipService.GetMyMembershipAsync(userId);
        Assert.Null(result);

        clientRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task BuyMembership_MissingClient_Throws(
        [Frozen] IClientRepository clientRepo,
        MembershipService membershipService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Client?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.BuyMembershipAsync(userId, new BuyMembershipRequest(1, 30, PaymentMethod.Cash)));

        Assert.Equal("Client profile not found.", exception.Message);
        clientRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task BuyMembership_MissingPlan_Throws(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        var membershipPlanRepositoryMock = Mock.Get(planRepo);

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

        membershipPlanRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((MembershipPlan?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.BuyMembershipAsync(userId, new BuyMembershipRequest(1, 30, PaymentMethod.Card)));

        Assert.Equal("Membership plan not found.", exception.Message);
        clientRepositoryMock.VerifyAll();
        membershipPlanRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task BuyMembership_BadDuration_Throws(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        var membershipPlanRepositoryMock = Mock.Get(planRepo);

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

        membershipPlanRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new MembershipPlan
        {
            Id = 1, Name = "Base", Description = "Standard access", Durations = [30, 90], Price = 1000
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.BuyMembershipAsync(userId, new BuyMembershipRequest(1, 365, PaymentMethod.Card)));

        Assert.Equal("Selected duration is not allowed for this plan.", exception.Message);
        clientRepositoryMock.VerifyAll();
        membershipPlanRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task BuyMembership_New_CreatesMembershipAndPayment(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        [Frozen] IMembershipRepository membershipRepo,
        [Frozen] IPaymentRepository paymentRepo,
        MembershipService membershipService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        var membershipPlanRepositoryMock = Mock.Get(planRepo);
        var membershipRepositoryMock = Mock.Get(membershipRepo);
        var paymentRepositoryMock = Mock.Get(paymentRepo);

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

        var membershipPlan = new MembershipPlan
            { Id = 7, Name = "Base", Description = "Standard access", Durations = [30], Price = 1500 };

        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);

        membershipPlanRepositoryMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(membershipPlan);

        membershipRepositoryMock.Setup(r => r.GetByClientIdAsync(client.Id)).ReturnsAsync((Membership?)null);

        Membership? addedMembership = null;
        membershipRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Membership>()))
            .Callback<Membership>(membership => addedMembership = membership)
            .Returns(Task.CompletedTask);

        Payment? addedPayment = null;
        paymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .Callback<Payment>(payment => addedPayment = payment)
            .Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var result =
            await membershipService.BuyMembershipAsync(userId, new BuyMembershipRequest(7, 30, PaymentMethod.Cash));
        var after = DateTime.UtcNow;

        Assert.NotNull(addedMembership);
        Assert.Equal(client.Id, addedMembership!.ClientId);
        Assert.Equal(membershipPlan.Id, addedMembership.PlanId);
        Assert.Equal(MembershipStatus.Active, addedMembership.Status);
        Assert.True(addedMembership.ActivatedDate >= before.AddSeconds(-5) &&
                    addedMembership.ActivatedDate <= after.AddSeconds(5));
        Assert.Equal(addedMembership.ActivatedDate.AddDays(30).Date, addedMembership.ExpireDate.Date);

        Assert.NotNull(addedPayment);
        Assert.Equal(membershipPlan.Price, addedPayment!.Amount);
        Assert.Equal(PaymentStatus.Completed, addedPayment.Status);
        Assert.Equal(PaymentMethod.Cash, addedPayment.Method);
        Assert.Equal(client.Id, addedPayment.ClientId);
        Assert.Null(addedPayment.EmployeeId);
        Assert.NotNull(addedPayment.PaymentDate);
        Assert.True(
            addedPayment.PaymentDate >= before.AddSeconds(-5) && addedPayment.PaymentDate <= after.AddSeconds(5));

        Assert.Equal(addedMembership.PlanId, result.PlanId);
        Assert.Equal(addedMembership.Status, result.Status);

        clientRepositoryMock.VerifyAll();
        membershipPlanRepositoryMock.VerifyAll();
        membershipRepositoryMock.VerifyAll();
        paymentRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task BuyMembership_Existing_UpdatesMembershipAndAddsPayment(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        [Frozen] IMembershipRepository membershipRepo,
        [Frozen] IPaymentRepository paymentRepo,
        MembershipService membershipService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepo);
        var membershipPlanRepositoryMock = Mock.Get(planRepo);
        var membershipRepositoryMock = Mock.Get(membershipRepo);
        var paymentRepositoryMock = Mock.Get(paymentRepo);

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

        var membershipPlan = new MembershipPlan
            { Id = 9, Name = "Pro", Description = "Unlimited access", Durations = [], Price = 2500 };
        var existingMembership = new Membership
        {
            Id = 123,
            ClientId = client.Id,
            PlanId = 1,
            Status = MembershipStatus.Expired,
            ActivatedDate = new DateTime(2020, 1, 1),
            ExpireDate = new DateTime(2020, 2, 1)
        };

        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);

        membershipPlanRepositoryMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(membershipPlan);

        membershipRepositoryMock.Setup(r => r.GetByClientIdAsync(client.Id)).ReturnsAsync(existingMembership);
        membershipRepositoryMock.Setup(r => r.UpdateAsync(existingMembership)).Returns(Task.CompletedTask);

        paymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        var result =
            await membershipService.BuyMembershipAsync(userId, new BuyMembershipRequest(9, 45, PaymentMethod.Card));

        Assert.Equal(123, result.Id);
        Assert.Equal(9, result.PlanId);
        Assert.Equal(MembershipStatus.Active, result.Status);

        clientRepositoryMock.VerifyAll();
        membershipPlanRepositoryMock.VerifyAll();
        membershipRepositoryMock.VerifyAll();
        paymentRepositoryMock.VerifyAll();
    }
}