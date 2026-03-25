using ZPassFit.Data.Models.Attendance;

namespace ZPassFit.Data.Repositories.Attendance;

public interface IQrSessionRepository
{
    Task<QrSession?> GetByTokenAsync(Guid token);
    Task AddAsync(QrSession qrSession);
    Task UpdateAsync(QrSession qrSession);
    Task DeleteByTokenAsync(Guid token);
}