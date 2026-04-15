using ZPassFit.Data.Models.Memberships;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class MembershipService(
    IClientRepository clientRepository,
    IMembershipPlanRepository planRepository,
    IMembershipRepository membershipRepository,
    IPaymentRepository paymentRepository
) : IMembershipService
{
    public async Task<IEnumerable<MembershipPlanResponse>> GetPlansAsync()
    {
        var plans = await planRepository.GetAllAsync();
        return plans.Select(p => new MembershipPlanResponse(p.Id, p.Name, p.Description, p.Durations, p.Price));
    }

    public async Task<MembershipPlanResponse?> GetPlanByIdAsync(int id)
    {
        var plan = await planRepository.GetByIdAsync(id);
        return plan == null
            ? null
            : new MembershipPlanResponse(plan.Id, plan.Name, plan.Description, plan.Durations, plan.Price);
    }

    public async Task<MembershipResponse?> GetMyMembershipAsync(string userId)
    {
        var client = await clientRepository.GetByUserIdAsync(userId);
        if (client == null) return null;

        var membership = await membershipRepository.GetByClientIdAsync(client.Id);
        return membership == null ? null : MapMembership(membership);
    }

    public async Task<MembershipResponse> BuyMembershipAsync(string userId, BuyMembershipRequest request)
    {
        var client = await clientRepository.GetByUserIdAsync(userId)
                     ?? throw new InvalidOperationException("Client profile not found.");

        var plan = await planRepository.GetByIdAsync(request.PlanId)
                   ?? throw new InvalidOperationException("Membership plan not found.");

        if (plan.Durations.Length > 0 && !plan.Durations.Contains(request.DurationDays))
            throw new InvalidOperationException("Selected duration is not allowed for this plan.");

        var now = DateTime.UtcNow;
        var membership = await membershipRepository.GetByClientIdAsync(client.Id);

        if (membership == null)
        {
            membership = new Membership
            {
                ClientId = client.Id,
                PlanId = plan.Id,
                Status = MembershipStatus.Active,
                ActivatedDate = now,
                ExpireDate = now.AddDays(request.DurationDays)
            };
            await membershipRepository.AddAsync(membership);
        }
        else
        {
            membership.PlanId = plan.Id;
            membership.Status = MembershipStatus.Active;
            membership.ActivatedDate = now;
            membership.ExpireDate = now.AddDays(request.DurationDays);
            await membershipRepository.UpdateAsync(membership);
        }

        var payment = new Payment
        {
            Amount = plan.Price,
            Method = request.Method,
            Status = PaymentStatus.Completed,
            PaymentDate = now,
            ClientId = client.Id,
            EmployeeId = null
        };
        await paymentRepository.AddAsync(payment);

        return MapMembership(membership);
    }

    public async Task<IEnumerable<PaymentResponse>> GetMyPaymentsAsync(string userId)
    {
        var client = await clientRepository.GetByUserIdAsync(userId);
        if (client == null) return [];

        var payments = await paymentRepository.GetByClientIdAsync(client.Id);
        return payments.Select(p =>
            new PaymentResponse(p.Id, p.Amount, p.Method, p.Status, p.CreateDate, p.PaymentDate));
    }

    public async Task<MembershipPlanResponse> CreatePlanAsync(CreateMembershipPlanRequest request)
    {
        ValidateDurations(request.Durations);

        var plan = new MembershipPlan
        {
            Name = request.Name,
            Description = request.Description,
            Durations = request.Durations,
            Price = request.Price
        };

        await planRepository.AddAsync(plan);
        return new MembershipPlanResponse(plan.Id, plan.Name, plan.Description, plan.Durations, plan.Price);
    }

    public async Task<MembershipPlanResponse?> UpdatePlanAsync(int id, UpdateMembershipPlanRequest request)
    {
        var plan = await planRepository.GetByIdAsync(id);
        if (plan == null) return null;

        ValidateDurations(request.Durations);

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.Durations = request.Durations;
        plan.Price = request.Price;

        await planRepository.UpdateAsync(plan);
        return new MembershipPlanResponse(plan.Id, plan.Name, plan.Description, plan.Durations, plan.Price);
    }

    public async Task DeletePlanAsync(int id)
    {
        var plan = await planRepository.GetByIdAsync(id)
                   ?? throw new InvalidOperationException("Membership plan not found.");

        var inUse = await membershipRepository.CountByPlanIdAsync(plan.Id);
        if (inUse > 0)
            throw new InvalidOperationException("Cannot delete a plan that is assigned to memberships.");

        await planRepository.DeleteAsync(id);
    }

    public async Task<MembershipListItemResponse> AdminSetMembershipAsync(AdminSetMembershipRequest request)
    {
        var client = await clientRepository.GetByIdAsync(request.ClientId)
                     ?? throw new InvalidOperationException("Client not found.");

        var plan = await planRepository.GetByIdAsync(request.PlanId)
                   ?? throw new InvalidOperationException("Membership plan not found.");

        if (plan.Durations.Length > 0 && !plan.Durations.Contains(request.DurationDays))
            throw new InvalidOperationException("Selected duration is not allowed for this plan.");

        var now = DateTime.UtcNow;
        var membership = await membershipRepository.GetByClientIdAsync(client.Id);

        if (membership == null)
        {
            membership = new Membership
            {
                ClientId = client.Id,
                PlanId = plan.Id,
                Status = MembershipStatus.Active,
                ActivatedDate = now,
                ExpireDate = now.AddDays(request.DurationDays)
            };
            await membershipRepository.AddAsync(membership);
        }
        else
        {
            membership.PlanId = plan.Id;
            membership.Status = MembershipStatus.Active;
            membership.ActivatedDate = now;
            membership.ExpireDate = now.AddDays(request.DurationDays);
            await membershipRepository.UpdateAsync(membership);
        }

        var reloaded = await membershipRepository.GetByIdAsync(membership.Id)
                       ?? throw new InvalidOperationException("Membership not found after save.");
        return MapListItem(reloaded);
    }

    public async Task<MembershipListItemResponse?> AdminUpdateMembershipAsync(int id, UpdateMembershipRequest request)
    {
        var membership = await membershipRepository.GetByIdAsync(id);
        if (membership == null) return null;

        if (request.PlanId is { } planId)
        {
            var plan = await planRepository.GetByIdAsync(planId)
                       ?? throw new InvalidOperationException("Membership plan not found.");
            membership.PlanId = plan.Id;
        }

        if (request.Status is { } status)
            membership.Status = status;

        if (request.ActivatedDate != null || request.ExpireDate != null)
        {
            var activated = request.ActivatedDate ?? membership.ActivatedDate;
            var expire = request.ExpireDate ?? membership.ExpireDate;
            if (expire <= activated)
                throw new InvalidOperationException("ExpireDate must be after ActivatedDate.");

            membership.ActivatedDate = activated;
            membership.ExpireDate = expire;
        }

        await membershipRepository.UpdateAsync(membership);

        var reloaded = await membershipRepository.GetByIdAsync(id);
        return reloaded == null ? null : MapListItem(reloaded);
    }

    private static void ValidateDurations(int[] durations)
    {
        foreach (var d in durations)
        {
            if (d <= 0)
                throw new InvalidOperationException("Each duration must be a positive number of days.");
        }
    }

    private static MembershipResponse MapMembership(Membership m)
    {
        return new MembershipResponse(m.Id, m.PlanId, m.Status, m.ActivatedDate, m.ExpireDate);
    }

    private static MembershipListItemResponse MapListItem(Membership m)
    {
        return new MembershipListItemResponse(
            m.Id,
            m.PlanId,
            m.Plan.Name,
            m.ClientId,
            m.Client.LastName,
            m.Client.FirstName,
            m.Status,
            m.ActivatedDate,
            m.ExpireDate
        );
    }
}