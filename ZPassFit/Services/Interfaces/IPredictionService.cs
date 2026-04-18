using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IPredictionService
{
    Task<ChurnPredictionResponse?> PredictChurnAsync(Guid clientId, CancellationToken cancellationToken);
}
