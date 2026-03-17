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
        return payments.Select(p => new PaymentResponse(p.Id, p.Amount, p.Method, p.Status, p.CreateDate, p.PaymentDate));
    }

    private static MembershipResponse MapMembership(Membership m) =>
        new(m.Id, m.PlanId, m.Status, m.ActivatedDate, m.ExpireDate);
}