using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IAttendanceService
{
    Task<QrSessionResponse> CreateQrSessionAsync(string userId, TimeSpan? ttl = null);
    Task<VisitLogResponse?> GetOpenVisitAsync(string userId);
    Task<IEnumerable<VisitLogResponse>> GetVisitHistoryAsync(string userId);
    Task<VisitLogResponse> CheckInByTokenAsync(Guid token);
    Task<VisitLogResponse> CheckOutAsync(string userId);
}