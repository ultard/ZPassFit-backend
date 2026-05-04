using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IMembershipService
{
    Task<IEnumerable<MembershipPlanResponse>> GetPlansAsync();
    Task<MembershipPlanResponse?> GetPlanByIdAsync(Guid id);
    Task<MembershipResponse?> GetMyMembershipAsync(string userId);
    Task<MembershipResponse> BuyMembershipAsync(string userId, BuyMembershipRequest request);
    Task<MembershipResponse> CancelMembershipAsync(string userId);
    Task<IEnumerable<PaymentResponse>> GetMyPaymentsAsync(string userId);

    Task<MembershipPlanResponse> CreatePlanAsync(CreateMembershipPlanRequest request);
    Task<MembershipPlanResponse?> UpdatePlanAsync(Guid id, UpdateMembershipPlanRequest request);
    Task DeletePlanAsync(Guid id);

    Task<MembershipListItemResponse> AdminSetMembershipAsync(AdminSetMembershipRequest request);
    Task<MembershipListItemResponse?> AdminUpdateMembershipAsync(Guid id, UpdateMembershipRequest request);
}