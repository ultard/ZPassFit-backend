using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IMembershipService
{
    Task<IEnumerable<MembershipPlanResponse>> GetPlansAsync();
    Task<MembershipPlanResponse?> GetPlanByIdAsync(int id);
    Task<MembershipResponse?> GetMyMembershipAsync(string userId);
    Task<MembershipResponse> BuyMembershipAsync(string userId, BuyMembershipRequest request);
    Task<IEnumerable<PaymentResponse>> GetMyPaymentsAsync(string userId);

    Task<MembershipPlanResponse> CreatePlanAsync(CreateMembershipPlanRequest request);
    Task<MembershipPlanResponse?> UpdatePlanAsync(int id, UpdateMembershipPlanRequest request);
    Task DeletePlanAsync(int id);

    Task<MembershipListItemResponse> AdminSetMembershipAsync(AdminSetMembershipRequest request);
    Task<MembershipListItemResponse?> AdminUpdateMembershipAsync(int id, UpdateMembershipRequest request);
}