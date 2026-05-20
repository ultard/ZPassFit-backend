using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.IntegrationTest.Infrastructure;

/// <summary>
///     Заглушка ИИ-модуля для сквозных тестов бизнес-процесса «анализ и рекомендация».
/// </summary>
public sealed class StubPredictionService(
    IClientRepository clientRepository,
    IMembershipRepository membershipRepository
) : IPredictionService
{
    public const double StubProbability = 0.42;

    public async Task<ChurnPredictionResponse?> PredictChurnAsync(
        Guid clientId,
        CancellationToken cancellationToken
    )
    {
        var client = await clientRepository.GetByIdAsync(clientId);
        if (client is null)
            return null;

        var membership = await membershipRepository.GetByClientIdAsync(clientId);
        if (membership is null)
            return null;

        return new ChurnPredictionResponse(0, StubProbability);
    }
}