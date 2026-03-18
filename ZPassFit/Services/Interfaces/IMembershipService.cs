using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IMembershipService
{
    Task<IEnumerable<MembershipPlanResponse>> GetPlansAsync();
    Task<MembershipResponse?> GetMyMembershipAsync(string userId);
    Task<MembershipResponse> BuyMembershipAsync(string userId, BuyMembershipRequest request);
    Task<IEnumerable<PaymentResponse>> GetMyPaymentsAsync(string userId);
}