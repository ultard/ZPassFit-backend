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

    [Theory]
    [AutoMoqData]
    public async Task GetPlanById_Missing_ReturnsNull(
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var planMock = Mock.Get(planRepo);
        planMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((MembershipPlan?)null);

        var result = await membershipService.GetPlanByIdAsync(5);

        Assert.Null(result);
        planMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetPlanById_Maps(
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var planMock = Mock.Get(planRepo);
        planMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(
            new MembershipPlan
            {
                Id = 3,
                Name = "Gold",
                Description = "D",
                Durations = [30],
                Price = 500
            });

        var result = await membershipService.GetPlanByIdAsync(3);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Id);
        Assert.Equal("Gold", result.Name);
        Assert.Equal("D", result.Description);
        Assert.Equal([30], result.Durations);
        Assert.Equal(500, result.Price);
        planMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetMyMembership_NoMembership_ReturnsNull(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var userId = "u1";
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        var clientMock = Mock.Get(clientRepo);
        var membershipMock = Mock.Get(membershipRepo);
        clientMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);
        membershipMock.Setup(r => r.GetByClientIdAsync(client.Id)).ReturnsAsync((Membership?)null);

        var result = await membershipService.GetMyMembershipAsync(userId);

        Assert.Null(result);
        clientMock.VerifyAll();
        membershipMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetMyMembership_Maps(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var userId = "u1";
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        var membership = new Membership
        {
            Id = 7,
            ClientId = client.Id,
            PlanId = 2,
            Status = MembershipStatus.Active,
            ActivatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpireDate = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var clientMock = Mock.Get(clientRepo);
        var membershipMock = Mock.Get(membershipRepo);
        clientMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);
        membershipMock.Setup(r => r.GetByClientIdAsync(client.Id)).ReturnsAsync(membership);

        var result = await membershipService.GetMyMembershipAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(7, result!.Id);
        Assert.Equal(2, result.PlanId);
        Assert.Equal(MembershipStatus.Active, result.Status);
        Assert.Equal(membership.ActivatedDate, result.ActivatedDate);
        Assert.Equal(membership.ExpireDate, result.ExpireDate);
        clientMock.VerifyAll();
        membershipMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetMyPayments_NoClient_ReturnsEmpty(
        [Frozen] IClientRepository clientRepo,
        MembershipService membershipService
    )
    {
        var clientMock = Mock.Get(clientRepo);
        clientMock.Setup(r => r.GetByUserIdAsync("x")).ReturnsAsync((Client?)null);

        var result = await membershipService.GetMyPaymentsAsync("x");

        Assert.Empty(result);
        clientMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetMyPayments_Maps(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IPaymentRepository paymentRepo,
        MembershipService membershipService
    )
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        var created = new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var paid = new DateTime(2025, 3, 2, 15, 0, 0, DateTimeKind.Utc);
        var payments = new[]
        {
            new Payment
            {
                Id = 1,
                Amount = 100,
                Method = PaymentMethod.Card,
                Status = PaymentStatus.Completed,
                CreateDate = created,
                PaymentDate = paid,
                ClientId = client.Id
            }
        };

        var clientMock = Mock.Get(clientRepo);
        var paymentMock = Mock.Get(paymentRepo);
        clientMock.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(client);
        paymentMock.Setup(r => r.GetByClientIdAsync(client.Id)).ReturnsAsync(payments);

        var result = (await membershipService.GetMyPaymentsAsync("u1")).ToArray();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(100, result[0].Amount);
        Assert.Equal(PaymentMethod.Card, result[0].Method);
        Assert.Equal(PaymentStatus.Completed, result[0].Status);
        Assert.Equal(created, result[0].CreateDate);
        Assert.Equal(paid, result[0].PaymentDate);
        clientMock.VerifyAll();
        paymentMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CreatePlan_AddsAndMaps(
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var planMock = Mock.Get(planRepo);
        planMock.Setup(r => r.AddAsync(It.IsAny<MembershipPlan>()))
            .Callback<MembershipPlan>(p => p.Id = 42)
            .Returns(Task.CompletedTask);

        var result = await membershipService.CreatePlanAsync(
            new CreateMembershipPlanRequest("Name", "Desc", [30, 60], 900));

        Assert.Equal(42, result.Id);
        Assert.Equal("Name", result.Name);
        Assert.Equal("Desc", result.Description);
        Assert.Equal([30, 60], result.Durations);
        Assert.Equal(900, result.Price);
        planMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CreatePlan_InvalidDuration_Throws(
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var planMock = Mock.Get(planRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.CreatePlanAsync(new CreateMembershipPlanRequest("N", "D", [10, 0], 100)));

        Assert.Equal("Each duration must be a positive number of days.", ex.Message);
        planMock.Verify(r => r.AddAsync(It.IsAny<MembershipPlan>()), Times.Never);
    }

    [Theory]
    [AutoMoqData]
    public async Task UpdatePlan_Missing_ReturnsNull(
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var planMock = Mock.Get(planRepo);
        planMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((MembershipPlan?)null);

        var result = await membershipService.UpdatePlanAsync(
            1,
            new UpdateMembershipPlanRequest("N", "D", [30], 200));

        Assert.Null(result);
        planMock.Verify(r => r.UpdateAsync(It.IsAny<MembershipPlan>()), Times.Never);
        planMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task UpdatePlan_UpdatesAndMaps(
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var existing = new MembershipPlan
        {
            Id = 5,
            Name = "Old",
            Description = "O",
            Durations = [30],
            Price = 100
        };

        var planMock = Mock.Get(planRepo);
        planMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        planMock.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

        var result = await membershipService.UpdatePlanAsync(
            5,
            new UpdateMembershipPlanRequest("New", "ND", [90], 300));

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
        Assert.Equal("New", result.Name);
        Assert.Equal("ND", result.Description);
        Assert.Equal([90], result.Durations);
        Assert.Equal(300, result.Price);
        planMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task UpdatePlan_InvalidDuration_Throws(
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var existing = new MembershipPlan
        {
            Id = 5,
            Name = "Old",
            Description = "O",
            Durations = [30],
            Price = 100
        };

        var planMock = Mock.Get(planRepo);
        planMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.UpdatePlanAsync(5, new UpdateMembershipPlanRequest("N", "D", [-1], 200)));

        Assert.Equal("Each duration must be a positive number of days.", ex.Message);
        planMock.Verify(r => r.UpdateAsync(It.IsAny<MembershipPlan>()), Times.Never);
    }

    [Theory]
    [AutoMoqData]
    public async Task DeletePlan_Missing_Throws(
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var planMock = Mock.Get(planRepo);
        planMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((MembershipPlan?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => membershipService.DeletePlanAsync(9));

        Assert.Equal("Membership plan not found.", ex.Message);
        planMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [AutoMoqData]
    public async Task DeletePlan_InUse_Throws(
        [Frozen] IMembershipPlanRepository planRepo,
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var plan = new MembershipPlan
        {
            Id = 2,
            Name = "P",
            Description = "D",
            Durations = [30],
            Price = 1
        };

        var planMock = Mock.Get(planRepo);
        var membershipMock = Mock.Get(membershipRepo);
        planMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(plan);
        membershipMock.Setup(r => r.CountByPlanIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => membershipService.DeletePlanAsync(2));

        Assert.Equal("Cannot delete a plan that is assigned to memberships.", ex.Message);
        planMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        planMock.VerifyAll();
        membershipMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task DeletePlan_Deletes(
        [Frozen] IMembershipPlanRepository planRepo,
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var plan = new MembershipPlan
        {
            Id = 2,
            Name = "P",
            Description = "D",
            Durations = [30],
            Price = 1
        };

        var planMock = Mock.Get(planRepo);
        var membershipMock = Mock.Get(membershipRepo);
        planMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(plan);
        membershipMock.Setup(r => r.CountByPlanIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        planMock.Setup(r => r.DeleteAsync(2)).Returns(Task.CompletedTask);

        await membershipService.DeletePlanAsync(2);

        planMock.Verify(r => r.DeleteAsync(2), Times.Once);
        planMock.VerifyAll();
        membershipMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminSetMembership_ClientMissing_Throws(
        [Frozen] IClientRepository clientRepo,
        MembershipService membershipService
    )
    {
        var clientId = Guid.NewGuid();
        var clientMock = Mock.Get(clientRepo);
        clientMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync((Client?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.AdminSetMembershipAsync(new AdminSetMembershipRequest(clientId, 1, 30)));

        Assert.Equal("Client not found.", ex.Message);
        clientMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminSetMembership_PlanMissing_Throws(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var clientId = Guid.NewGuid();
        var client = new Client
        {
            Id = clientId,
            UserId = "u",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        var clientMock = Mock.Get(clientRepo);
        var planMock = Mock.Get(planRepo);
        clientMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        planMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((MembershipPlan?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.AdminSetMembershipAsync(new AdminSetMembershipRequest(clientId, 99, 30)));

        Assert.Equal("Membership plan not found.", ex.Message);
        clientMock.VerifyAll();
        planMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminSetMembership_BadDuration_Throws(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var clientId = Guid.NewGuid();
        var client = new Client
        {
            Id = clientId,
            UserId = "u",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        var plan = new MembershipPlan
        {
            Id = 1,
            Name = "P",
            Description = "D",
            Durations = [30],
            Price = 100
        };

        var clientMock = Mock.Get(clientRepo);
        var planMock = Mock.Get(planRepo);
        clientMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        planMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(plan);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.AdminSetMembershipAsync(new AdminSetMembershipRequest(clientId, 1, 365)));

        Assert.Equal("Selected duration is not allowed for this plan.", ex.Message);
        clientMock.VerifyAll();
        planMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminSetMembership_New_Maps(
        [Frozen] IClientRepository clientRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var clientId = Guid.NewGuid();
        var client = new Client
        {
            Id = clientId,
            UserId = "u",
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "И.",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        var plan = new MembershipPlan
        {
            Id = 3,
            Name = "PlanX",
            Description = "D",
            Durations = [30],
            Price = 500
        };

        var clientMock = Mock.Get(clientRepo);
        var planMock = Mock.Get(planRepo);
        var membershipMock = Mock.Get(membershipRepo);

        clientMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        planMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(plan);
        membershipMock.Setup(r => r.GetByClientIdAsync(clientId)).ReturnsAsync((Membership?)null);
        membershipMock.Setup(r => r.AddAsync(It.IsAny<Membership>()))
            .Callback<Membership>(m => m.Id = 200)
            .Returns(Task.CompletedTask);

        var reloaded = new Membership
        {
            Id = 200,
            ClientId = clientId,
            Client = client,
            PlanId = 3,
            Plan = plan,
            Status = MembershipStatus.Active,
            ActivatedDate = DateTime.UtcNow,
            ExpireDate = DateTime.UtcNow.AddDays(30)
        };
        membershipMock.Setup(r => r.GetByIdAsync(200)).ReturnsAsync(reloaded);

        var result = await membershipService.AdminSetMembershipAsync(
            new AdminSetMembershipRequest(clientId, 3, 30));

        Assert.Equal(200, result.Id);
        Assert.Equal(3, result.PlanId);
        Assert.Equal("PlanX", result.PlanName);
        Assert.Equal(clientId, result.ClientId);
        Assert.Equal("Иванов", result.ClientLastName);
        Assert.Equal("Иван", result.ClientFirstName);
        Assert.Equal(MembershipStatus.Active, result.Status);
        clientMock.VerifyAll();
        planMock.VerifyAll();
        membershipMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminUpdateMembership_Missing_ReturnsNull(
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var membershipMock = Mock.Get(membershipRepo);
        membershipMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Membership?)null);

        var result = await membershipService.AdminUpdateMembershipAsync(
            999,
            new UpdateMembershipRequest(MembershipStatus.Frozen, null, null, null));

        Assert.Null(result);
        membershipMock.Verify(r => r.UpdateAsync(It.IsAny<Membership>()), Times.Never);
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminUpdateMembership_UpdatesPlan(
        [Frozen] IMembershipRepository membershipRepo,
        [Frozen] IMembershipPlanRepository planRepo,
        MembershipService membershipService
    )
    {
        var plan = new MembershipPlan
        {
            Id = 10,
            Name = "NewPlan",
            Description = "D",
            Durations = [30],
            Price = 1
        };

        var membership = new Membership
        {
            Id = 1,
            ClientId = Guid.NewGuid(),
            PlanId = 1,
            Status = MembershipStatus.Active,
            ActivatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpireDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var client = new Client
        {
            Id = membership.ClientId,
            UserId = "u",
            LastName = "L",
            FirstName = "F",
            MiddleName = "M",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        membership.Client = client;
        membership.Plan = new MembershipPlan
        {
            Id = 1,
            Name = "Old",
            Description = "D",
            Durations = [30],
            Price = 1
        };

        var membershipMock = Mock.Get(membershipRepo);
        var planMock = Mock.Get(planRepo);

        membershipMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(membership);
        planMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(plan);
        membershipMock.Setup(r => r.UpdateAsync(membership)).Returns(Task.CompletedTask);

        var reloaded = new Membership
        {
            Id = 1,
            ClientId = membership.ClientId,
            Client = client,
            PlanId = 10,
            Plan = plan,
            Status = MembershipStatus.Active,
            ActivatedDate = membership.ActivatedDate,
            ExpireDate = membership.ExpireDate
        };
        membershipMock.SetupSequence(r => r.GetByIdAsync(1)).ReturnsAsync(membership).ReturnsAsync(reloaded);

        var result = await membershipService.AdminUpdateMembershipAsync(
            1,
            new UpdateMembershipRequest(null, 10, null, null));

        Assert.NotNull(result);
        Assert.Equal(10, result!.PlanId);
        Assert.Equal("NewPlan", result.PlanName);
        membershipMock.VerifyAll();
        planMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminUpdateMembership_UpdatesStatus(
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u",
            LastName = "L",
            FirstName = "F",
            MiddleName = "M",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        var plan = new MembershipPlan
        {
            Id = 1,
            Name = "P",
            Description = "D",
            Durations = [30],
            Price = 1
        };

        var membership = new Membership
        {
            Id = 5,
            ClientId = client.Id,
            PlanId = 1,
            Status = MembershipStatus.Active,
            ActivatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpireDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            Client = client,
            Plan = plan
        };

        var membershipMock = Mock.Get(membershipRepo);
        membershipMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(membership);
        membershipMock.Setup(r => r.UpdateAsync(membership)).Returns(Task.CompletedTask);

        var frozen = new Membership
        {
            Id = 5,
            ClientId = client.Id,
            PlanId = 1,
            Status = MembershipStatus.Frozen,
            ActivatedDate = membership.ActivatedDate,
            ExpireDate = membership.ExpireDate,
            Client = client,
            Plan = plan
        };
        membershipMock.SetupSequence(r => r.GetByIdAsync(5)).ReturnsAsync(membership).ReturnsAsync(frozen);

        var result = await membershipService.AdminUpdateMembershipAsync(
            5,
            new UpdateMembershipRequest(MembershipStatus.Frozen, null, null, null));

        Assert.NotNull(result);
        Assert.Equal(MembershipStatus.Frozen, result!.Status);
        membershipMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminUpdateMembership_DatesInvalid_Throws(
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var membership = new Membership
        {
            Id = 1,
            ClientId = Guid.NewGuid(),
            PlanId = 1,
            Status = MembershipStatus.Active,
            ActivatedDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpireDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var membershipMock = Mock.Get(membershipRepo);
        membershipMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(membership);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.AdminUpdateMembershipAsync(
                1,
                new UpdateMembershipRequest(null, null, new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc))));

        Assert.Equal("ExpireDate must be after ActivatedDate.", ex.Message);
        membershipMock.Verify(r => r.UpdateAsync(It.IsAny<Membership>()), Times.Never);
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminUpdateMembership_UpdatesDates(
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u",
            LastName = "L",
            FirstName = "F",
            MiddleName = "M",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        var plan = new MembershipPlan
        {
            Id = 1,
            Name = "P",
            Description = "D",
            Durations = [30],
            Price = 1
        };

        var act = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var exp = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var membership = new Membership
        {
            Id = 8,
            ClientId = client.Id,
            PlanId = 1,
            Status = MembershipStatus.Active,
            ActivatedDate = act,
            ExpireDate = exp,
            Client = client,
            Plan = plan
        };

        var newAct = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var newExp = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        var membershipMock = Mock.Get(membershipRepo);
        membershipMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(membership);
        membershipMock.Setup(r => r.UpdateAsync(membership)).Returns(Task.CompletedTask);

        var reloaded = new Membership
        {
            Id = 8,
            ClientId = client.Id,
            PlanId = 1,
            Status = MembershipStatus.Active,
            ActivatedDate = newAct,
            ExpireDate = newExp,
            Client = client,
            Plan = plan
        };
        membershipMock.SetupSequence(r => r.GetByIdAsync(8)).ReturnsAsync(membership).ReturnsAsync(reloaded);

        var result = await membershipService.AdminUpdateMembershipAsync(
            8,
            new UpdateMembershipRequest(null, null, newAct, newExp));

        Assert.NotNull(result);
        Assert.Equal(newAct, result!.ActivatedDate);
        Assert.Equal(newExp, result.ExpireDate);
        membershipMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task AdminUpdateMembership_ReloadMissing_ReturnsNull(
        [Frozen] IMembershipRepository membershipRepo,
        MembershipService membershipService
    )
    {
        var membership = new Membership
        {
            Id = 3,
            ClientId = Guid.NewGuid(),
            PlanId = 1,
            Status = MembershipStatus.Active,
            ActivatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpireDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var membershipMock = Mock.Get(membershipRepo);
        membershipMock.SetupSequence(r => r.GetByIdAsync(3)).ReturnsAsync(membership).ReturnsAsync((Membership?)null);
        membershipMock.Setup(r => r.UpdateAsync(membership)).Returns(Task.CompletedTask);

        var result = await membershipService.AdminUpdateMembershipAsync(
            3,
            new UpdateMembershipRequest(MembershipStatus.Disabled, null, null, null));

        Assert.Null(result);
        membershipMock.VerifyAll();
    }
}