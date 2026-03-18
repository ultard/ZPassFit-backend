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
    [Fact]
    public async Task GetPlansAsync_MapsPlans()
    {
        var planRepo = new Mock<IMembershipPlanRepository>(MockBehavior.Strict);
        planRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new MembershipPlan
            {
                Id = 1, 
                Name = "Base",
                Description = "Desc",
                Durations = [30, 90],
                Price = 1000
            }
        ]);

        var svc = new MembershipService(
            clientRepository: Mock.Of<IClientRepository>(),
            planRepository: planRepo.Object,
            membershipRepository: Mock.Of<IMembershipRepository>(),
            paymentRepository: Mock.Of<IPaymentRepository>()
        );

        var plans = (await svc.GetPlansAsync()).ToArray();
        Assert.Single(plans);
        Assert.Equal(1, plans[0].Id);
        Assert.Equal("Base", plans[0].Name);
        Assert.Equal("Desc", plans[0].Description);
        Assert.Equal([30, 90], plans[0].Durations);
        Assert.Equal(1000, plans[0].Price);

        planRepo.VerifyAll();
    }

    [Fact]
    public async Task GetMyMembershipAsync_WhenClientMissing_ReturnsNull()
    {
        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        clientRepo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync((Client?)null);

        var svc = new MembershipService(
            clientRepository: clientRepo.Object,
            planRepository: Mock.Of<IMembershipPlanRepository>(),
            membershipRepository: Mock.Of<IMembershipRepository>(),
            paymentRepository: Mock.Of<IPaymentRepository>()
        );

        var res = await svc.GetMyMembershipAsync("u1");
        Assert.Null(res);

        clientRepo.VerifyAll();
    }

    [Fact]
    public async Task BuyMembershipAsync_WhenClientMissing_Throws()
    {
        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        clientRepo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync((Client?)null);

        var svc = new MembershipService(
            clientRepository: clientRepo.Object,
            planRepository: Mock.Of<IMembershipPlanRepository>(),
            membershipRepository: Mock.Of<IMembershipRepository>(),
            paymentRepository: Mock.Of<IPaymentRepository>()
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.BuyMembershipAsync("u1", new BuyMembershipRequest(PlanId: 1, DurationDays: 30, Method: PaymentMethod.Cash)));

        Assert.Equal("Client profile not found.", ex.Message);
        clientRepo.VerifyAll();
    }

    [Fact]
    public async Task BuyMembershipAsync_WhenPlanMissing_Throws()
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

        var planRepo = new Mock<IMembershipPlanRepository>(MockBehavior.Strict);
        planRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((MembershipPlan?)null);

        var svc = new MembershipService(
            clientRepository: clientRepo.Object,
            planRepository: planRepo.Object,
            membershipRepository: Mock.Of<IMembershipRepository>(),
            paymentRepository: Mock.Of<IPaymentRepository>()
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.BuyMembershipAsync("u1", new BuyMembershipRequest(PlanId: 1, DurationDays: 30, Method: PaymentMethod.Card)));

        Assert.Equal("Membership plan not found.", ex.Message);
        clientRepo.VerifyAll();
        planRepo.VerifyAll();
    }

    [Fact]
    public async Task BuyMembershipAsync_WhenDurationNotAllowed_Throws()
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

        var planRepo = new Mock<IMembershipPlanRepository>(MockBehavior.Strict);
        planRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new MembershipPlan
        {
            Id = 1, Name = "Base", Description = "Desc", Durations = [30, 90], Price = 1000
        });

        var svc = new MembershipService(
            clientRepository: clientRepo.Object,
            planRepository: planRepo.Object,
            membershipRepository: Mock.Of<IMembershipRepository>(),
            paymentRepository: Mock.Of<IPaymentRepository>()
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.BuyMembershipAsync("u1", new BuyMembershipRequest(PlanId: 1, DurationDays: 365, Method: PaymentMethod.Card)));

        Assert.Equal("Selected duration is not allowed for this plan.", ex.Message);
        clientRepo.VerifyAll();
        planRepo.VerifyAll();
    }

    [Fact]
    public async Task BuyMembershipAsync_WhenNoExistingMembership_CreatesMembership_AndPayment()
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

        var plan = new MembershipPlan { Id = 7, Name = "Base", Description = "Desc", Durations = [30], Price = 1500 };

        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        clientRepo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(client);

        var planRepo = new Mock<IMembershipPlanRepository>(MockBehavior.Strict);
        planRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(plan);

        var membershipRepo = new Mock<IMembershipRepository>(MockBehavior.Strict);
        membershipRepo.Setup(r => r.GetByClientIdAsync(client.Id)).ReturnsAsync((Membership?)null);

        Membership? addedMembership = null;
        membershipRepo.Setup(r => r.AddAsync(It.IsAny<Membership>()))
            .Callback<Membership>(m => addedMembership = m)
            .Returns(Task.CompletedTask);

        var paymentRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        Payment? addedPayment = null;
        paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .Callback<Payment>(p => addedPayment = p)
            .Returns(Task.CompletedTask);

        var svc = new MembershipService(clientRepo.Object, planRepo.Object, membershipRepo.Object, paymentRepo.Object);

        var before = DateTime.UtcNow;
        var res = await svc.BuyMembershipAsync("u1", new BuyMembershipRequest(PlanId: 7, DurationDays: 30, Method: PaymentMethod.Cash));
        var after = DateTime.UtcNow;

        Assert.NotNull(addedMembership);
        Assert.Equal(client.Id, addedMembership!.ClientId);
        Assert.Equal(plan.Id, addedMembership.PlanId);
        Assert.Equal(MembershipStatus.Active, addedMembership.Status);
        Assert.True(addedMembership.ActivatedDate >= before.AddSeconds(-5) && addedMembership.ActivatedDate <= after.AddSeconds(5));
        Assert.Equal(addedMembership.ActivatedDate.AddDays(30).Date, addedMembership.ExpireDate.Date);

        Assert.NotNull(addedPayment);
        Assert.Equal(plan.Price, addedPayment!.Amount);
        Assert.Equal(PaymentStatus.Completed, addedPayment.Status);
        Assert.Equal(PaymentMethod.Cash, addedPayment.Method);
        Assert.Equal(client.Id, addedPayment.ClientId);
        Assert.Null(addedPayment.EmployeeId);
        Assert.NotNull(addedPayment.PaymentDate);
        Assert.True(addedPayment.PaymentDate >= before.AddSeconds(-5) && addedPayment.PaymentDate <= after.AddSeconds(5));

        Assert.Equal(addedMembership.PlanId, res.PlanId);
        Assert.Equal(addedMembership.Status, res.Status);

        clientRepo.VerifyAll();
        planRepo.VerifyAll();
        membershipRepo.VerifyAll();
        paymentRepo.VerifyAll();
    }

    [Fact]
    public async Task BuyMembershipAsync_WhenMembershipExists_UpdatesMembership_AndPayment()
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

        var plan = new MembershipPlan { Id = 9, Name = "Pro", Description = "Desc", Durations = [], Price = 2500 };
        var existing = new Membership
        {
            Id = 123,
            ClientId = client.Id,
            PlanId = 1,
            Status = MembershipStatus.Expired,
            ActivatedDate = new DateTime(2020, 1, 1),
            ExpireDate = new DateTime(2020, 2, 1)
        };

        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        clientRepo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(client);

        var planRepo = new Mock<IMembershipPlanRepository>(MockBehavior.Strict);
        planRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(plan);

        var membershipRepo = new Mock<IMembershipRepository>(MockBehavior.Strict);
        membershipRepo.Setup(r => r.GetByClientIdAsync(client.Id)).ReturnsAsync(existing);
        membershipRepo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

        var paymentRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

        var svc = new MembershipService(clientRepo.Object, planRepo.Object, membershipRepo.Object, paymentRepo.Object);
        var res = await svc.BuyMembershipAsync("u1", new BuyMembershipRequest(PlanId: 9, DurationDays: 45, Method: PaymentMethod.Card));

        Assert.Equal(123, res.Id);
        Assert.Equal(9, res.PlanId);
        Assert.Equal(MembershipStatus.Active, res.Status);

        clientRepo.VerifyAll();
        planRepo.VerifyAll();
        membershipRepo.VerifyAll();
        paymentRepo.VerifyAll();
    }
}

