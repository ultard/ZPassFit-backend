using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Protos;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class PredictionService(
    Protos.PredictionService.PredictionServiceClient predictionClient,
    IClientRepository clientRepository,
    IMembershipRepository membershipRepository,
    IVisitLogRepository visitLogRepository
) : IPredictionService
{
    public async Task<PredictClientResponse?> PredictAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(clientId);
        if (client == null)
        {
            return null;
        }

        var membership = await membershipRepository.GetByClientIdAsync(clientId);
        if (membership == null)
        {
            return null;
        }

        var visitHistory = (await visitLogRepository.GetVisitHistoryByClientIdAsync(clientId)).ToList();
        var now = DateTime.UtcNow;
        var last7dBorder = now.AddDays(-7);
        var last4wBorder = now.AddDays(-28);
        var prev4wBorder = now.AddDays(-56);

        var visitsLast7d = visitHistory.Count(v => v.EnterDate >= last7dBorder);
        var visitsLast4w = visitHistory.Count(v => v.EnterDate >= last4wBorder);
        var visitsPrev4w = visitHistory.Count(v => v.EnterDate >= prev4wBorder && v.EnterDate < last4wBorder);
        var visitsPerWeek = visitsLast4w / 4.0;

        var lastVisitDate = visitHistory.Select(v => (DateTime?)v.EnterDate).Max();
        var daysSinceLastVisit = lastVisitDate.HasValue
            ? Math.Max(0, (int)(now.Date - lastVisitDate.Value.Date).TotalDays)
            : 365;

        var membershipDurationDays = Math.Max(1, (int)(membership.ExpireDate.Date - membership.ActivatedDate.Date).TotalDays);
        var membershipDaysToExpire = Math.Max(0, (int)(membership.ExpireDate.Date - now.Date).TotalDays);

        var grpcRequest = new PredictRequest
        {
            Gender = MapGender(client.Gender),
            Age = CalculateAge(client.BirthDate, now),
            VisitsPerWeek = visitsPerWeek,
            VisitsLast7D = visitsLast7d,
            VisitsLast4W = visitsLast4w,
            VisitsPrev4W = visitsPrev4w,
            DaysSinceLastVisit = daysSinceLastVisit,
            MembershipPrice = membership.Plan.Price,
            MembershipDurationDays = membershipDurationDays,
            MembershipDaysToExpire = membershipDaysToExpire
        };

        try
        {
            var grpcResponse = await predictionClient.PredictAsync(grpcRequest, cancellationToken: cancellationToken);
            return new PredictClientResponse(grpcResponse.Prediction, grpcResponse.ChurnProbability);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Prediction service unavailable.", exception);
        }
    }

    private static int CalculateAge(DateTime birthDate, DateTime now)
    {
        var age = now.Year - birthDate.Year;
        if (birthDate.Date > now.Date.AddYears(-age))
        {
            age--;
        }

        return Math.Max(0, age);
    }

    private static string MapGender(ClientGender gender)
    {
        return gender switch
        {
            ClientGender.Male => "MALE",
            ClientGender.Female => "FEMALE",
            _ => "UNKNOWN"
        };
    }
}
