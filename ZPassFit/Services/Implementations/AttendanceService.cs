using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class AttendanceService(
    IClientRepository clientRepository,
    IMembershipRepository membershipRepository,
    IQrSessionRepository qrSessionRepository,
    IVisitLogRepository visitLogRepository
) : IAttendanceService
{
    public async Task<QrSessionResponse> CreateQrSessionAsync(string userId, TimeSpan? ttl = null)
    {
        var client = await clientRepository.GetByUserIdAsync(userId)
                     ?? throw new InvalidOperationException("Client profile not found.");

        var now = DateTime.UtcNow;
        var expires = now.Add(ttl ?? TimeSpan.FromMinutes(3));

        var session = new QrSession
        {
            Token = Guid.NewGuid(),
            CreateDate = now,
            ExpireDate = expires,
            ClientId = client.Id
        };

        await qrSessionRepository.AddAsync(session);
        return new QrSessionResponse(session.Token, session.ExpireDate);
    }

    public async Task<VisitLogResponse?> GetOpenVisitAsync(string userId)
    {
        var client = await clientRepository.GetByUserIdAsync(userId);
        if (client == null) return null;

        var visit = await visitLogRepository.GetOpenVisitByClientIdAsync(client.Id);
        return visit == null ? null : MapVisit(visit);
    }

    public async Task<IEnumerable<VisitLogResponse>> GetVisitHistoryAsync(string userId)
    {
        var client = await clientRepository.GetByUserIdAsync(userId);
        if (client == null) return [];

        var visits = await visitLogRepository.GetVisitHistoryByClientIdAsync(client.Id);
        return visits.Select(MapVisit);
    }

    public async Task<VisitLogResponse> CheckInByTokenAsync(Guid token)
    {
        var session = await qrSessionRepository.GetByTokenAsync(token)
                      ?? throw new InvalidOperationException("QR session not found.");

        if (session.ExpireDate < DateTime.UtcNow)
            throw new InvalidOperationException("QR session expired.");

        var open = await visitLogRepository.GetOpenVisitByClientIdAsync(session.ClientId);
        if (open != null)
            return MapVisit(open);

        var membership = await membershipRepository.GetByClientIdAsync(session.ClientId)
                         ?? throw new InvalidOperationException("Client has no membership.");

        var visit = new VisitLog
        {
            ClientId = session.ClientId,
            MembershipId = membership.Id,
            EnterDate = DateTime.UtcNow
        };

        await visitLogRepository.AddAsync(visit);
        await qrSessionRepository.DeleteByTokenAsync(token);

        return MapVisit(visit);
    }

    public async Task<VisitLogResponse> CheckOutAsync(string userId)
    {
        var client = await clientRepository.GetByUserIdAsync(userId)
                     ?? throw new InvalidOperationException("Client profile not found.");

        var open = await visitLogRepository.GetOpenVisitByClientIdAsync(client.Id)
                   ?? throw new InvalidOperationException("Open visit not found.");

        open.LeaveDate = DateTime.UtcNow;
        await visitLogRepository.UpdateAsync(open);
        return MapVisit(open);
    }

    private static VisitLogResponse MapVisit(VisitLog v)
    {
        return new VisitLogResponse(v.Id, v.EnterDate, v.LeaveDate, v.MembershipId, v.ClientId);
    }
}