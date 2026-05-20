using AutoFixture.Xunit3;
using Microsoft.Extensions.Options;
using Moq;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Options.Payments;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class MembershipServiceTests
{
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
            { Id = Guid.NewGuid(), Name = "Base", Description = "Standard access", Durations = [30], Price = 1500 };

        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);

        membershipPlanRepositoryMock.Setup(r => r.GetByIdAsync(membershipPlan.Id)).ReturnsAsync(membershipPlan);

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
            await membershipService.BuyMembershipAsync(
                userId,
                new BuyMembershipRequest(membershipPlan.Id, 30, PaymentMethod.Cash)
            );
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
    public async Task BuyMembership_Balance_ChecksScaledPrice(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var userId = "u1";
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
            Email = "ivan@example.com",
            Balance = 2000
        };

        var plan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            Name = "Base",
            Description = "Standard access",
            Durations = [30, 90],
            Price = 1500
        };

        Mock.Get(clientRepo).Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);
        Mock.Get(planRepo).Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        Mock.Get(membershipRepo).Setup(r => r.GetByClientIdAsync(client.Id)).ReturnsAsync((Membership?)null);
        Mock.Get(membershipRepo).Setup(r => r.AddAsync(It.IsAny<Membership>()))
            .Returns(Task.CompletedTask);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.BuyMembershipAsync(
                userId, new BuyMembershipRequest(plan.Id, 90, PaymentMethod.Balance)));

        Assert.Equal("Not enough balance.", ex.Message);
    }

    [Theory]
    [AutoMoqData]
    public async Task BuyMembership_DisabledMethod_Throws(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        [Frozen] IMembershipRepository membershipRepo,
        [Frozen] IPaymentRepository paymentRepo,
        Mock<IOptions<PaymentMethodsOptions>> paymentMethodsOptionsMock
    )
    {
        paymentMethodsOptionsMock
            .Setup(o => o.Value)
            .Returns(
                new PaymentMethodsOptions
                {
                    CashEnabled = false,
                    CardEnabled = true,
                    BalanceEnabled = true
                }
            );

        var membershipService = new MembershipService(
            clientRepo,
            planRepo,
            membershipRepo,
            paymentRepo,
            paymentMethodsOptionsMock.Object);

        var userId = "u1";
        var planId = Guid.NewGuid();
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

        var plan = new MembershipPlan
        {
            Id = planId,
            Name = "Base",
            Description = "Standard access",
            Durations = [30],
            Price = 1500
        };

        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);
        membershipPlanRepositoryMock.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(plan);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.BuyMembershipAsync(userId, new BuyMembershipRequest(planId, 30, PaymentMethod.Cash)));

        Assert.Equal("This payment method is disabled.", exception.Message);
        clientRepositoryMock.VerifyAll();
        membershipPlanRepositoryMock.VerifyAll();
    }
}