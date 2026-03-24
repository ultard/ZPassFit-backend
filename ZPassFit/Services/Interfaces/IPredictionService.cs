using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IPredictionService
{
    Task<PredictClientResponse?> PredictAsync(Guid clientId, CancellationToken cancellationToken);
}
