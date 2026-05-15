using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZPassFit.Dto;
using ZPassFit.IntegrationTest.Infrastructure;

namespace ZPassFit.IntegrationTest.BusinessProcess;

/// <summary>
/// БП 3: Анализ и рекомендация (прогноз оттока через ИИ-модуль).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ChurnRecommendationProcessTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task PredictChurn_ForClientWithHistory_ReturnsAiRecommendation()
    {
        var adminToken = await _client.LoginAsync("admin@dev.local", "DevPassword123!");

        var clientsResponse = await _client.GetAuthenticatedAsync(
            adminToken,
            "/dashboard/clients?search=client3@dev.local&pageSize=5"
        );
        clientsResponse.EnsureSuccessStatusCode();
        var clients = await clientsResponse.Content.ReadFromJsonAsync<PagedClientsResponse>();
        Assert.NotNull(clients);
        var client = Assert.Single(clients.Items);

        var levelBefore = await _client.GetAuthenticatedAsync(
            adminToken,
            $"/dashboard/clients/{client.Id}"
        );
        levelBefore.EnsureSuccessStatusCode();

        var predictResponse = await _client.PostAuthenticatedJsonAsync(
            adminToken,
            "/prediction/churn",
            new ChurnPredictionRequest(client.Id)
        );
        Assert.Equal(HttpStatusCode.OK, predictResponse.StatusCode);

        var prediction = await predictResponse.Content.ReadFromJsonAsync<ChurnPredictionResponse>();
        Assert.NotNull(prediction);
        Assert.Equal(0, prediction.Prediction);
        Assert.Equal(StubPredictionService.StubProbability, prediction.Probability, precision: 5);
    }

    [Fact]
    public async Task PredictChurn_ClientWithoutMembership_ReturnsNotFound()
    {
        var adminToken = await _client.LoginAsync("admin@dev.local", "DevPassword123!");

        var clientsResponse = await _client.GetAuthenticatedAsync(
            adminToken,
            "/dashboard/clients?search=client2@dev.local&pageSize=5"
        );
        clientsResponse.EnsureSuccessStatusCode();
        var clients = await clientsResponse.Content.ReadFromJsonAsync<PagedClientsResponse>();
        Assert.NotNull(clients);
        var client = Assert.Single(clients.Items);

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZPassFit.Data.ApplicationDbContext>();
            var membership = await db.Memberships.SingleOrDefaultAsync(m => m.ClientId == client.Id);
            if (membership is not null)
            {
                db.Memberships.Remove(membership);
                await db.SaveChangesAsync();
            }
        }

        var predictResponse = await _client.PostAuthenticatedJsonAsync(
            adminToken,
            "/prediction/churn",
            new ChurnPredictionRequest(client.Id)
        );

        Assert.Equal(HttpStatusCode.NotFound, predictResponse.StatusCode);
    }
}
