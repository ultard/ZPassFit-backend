using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZPassFit.Data;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Dto;
using ZPassFit.IntegrationTest.Infrastructure;

namespace ZPassFit.IntegrationTest.BusinessProcess;

[Collection(IntegrationTestCollection.Name)]
public sealed class ChurnRecommendationProcessTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task PredictChurn_ForClientWithHistory_ReturnsAiRecommendation()
    {
        var ct = TestContext.Current.CancellationToken;
        var adminToken = await _client.LoginAsync("admin@dev.local", "DevPassword123!", ct);

        var clientsResponse = await _client.GetAuthenticatedAsync(
            adminToken,
            "/dashboard/clients?search=client3@dev.local&pageSize=5",
            ct
        );
        clientsResponse.EnsureSuccessStatusCode();
        var clients =
            await clientsResponse.Content.ReadFromJsonAsync<PagedClientsResponse>(ct);
        Assert.NotNull(clients);
        var client = Assert.Single(clients.Items);

        var levelBefore = await _client.GetAuthenticatedAsync(
            adminToken,
            $"/dashboard/clients/{client.Id}",
            ct
        );
        levelBefore.EnsureSuccessStatusCode();

        var predictResponse = await _client.PostAuthenticatedJsonAsync(
            adminToken,
            "/prediction/churn",
            new ChurnPredictionRequest(client.Id),
            ct
        );
        Assert.Equal(HttpStatusCode.OK, predictResponse.StatusCode);

        var prediction =
            await predictResponse.Content.ReadFromJsonAsync<ChurnPredictionResponse>(ct);
        Assert.NotNull(prediction);
        Assert.Equal(0, prediction.Prediction);
        Assert.Equal(StubPredictionService.StubProbability, prediction.Probability, 5);
    }

    [Fact]
    public async Task PredictChurn_ClientWithoutMembership_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var adminToken = await _client.LoginAsync("admin@dev.local", "DevPassword123!", ct);

        var clientsResponse = await _client.GetAuthenticatedAsync(
            adminToken,
            "/dashboard/clients?search=client2@dev.local&pageSize=5",
            ct
        );
        clientsResponse.EnsureSuccessStatusCode();
        var clients =
            await clientsResponse.Content.ReadFromJsonAsync<PagedClientsResponse>(ct);
        Assert.NotNull(clients);
        var client = Assert.Single(clients.Items);

        Membership? removedMembership = null;
        try
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var membership = await db.Memberships.SingleOrDefaultAsync(m => m.ClientId == client.Id, ct);
                if (membership is not null)
                {
                    removedMembership = membership;
                    db.Memberships.Remove(membership);
                    await db.SaveChangesAsync(ct);
                }
            }

            var predictResponse = await _client.PostAuthenticatedJsonAsync(
                adminToken,
                "/prediction/churn",
                new ChurnPredictionRequest(client.Id),
                ct
            );

            Assert.Equal(HttpStatusCode.NotFound, predictResponse.StatusCode);
        }
        finally
        {
            if (removedMembership is not null)
            {
                using var scope = fixture.Factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hasMembership = await db.Memberships.AnyAsync(
                    m => m.ClientId == removedMembership.ClientId,
                    ct
                );
                if (!hasMembership)
                {
                    db.Memberships.Add(
                        new Membership
                        {
                            ClientId = removedMembership.ClientId,
                            PlanId = removedMembership.PlanId,
                            Status = removedMembership.Status,
                            ActivatedDate = removedMembership.ActivatedDate,
                            ExpireDate = removedMembership.ExpireDate,
                            AutoRenewEnabled = removedMembership.AutoRenewEnabled
                        }
                    );
                    await db.SaveChangesAsync(ct);
                }
            }
        }
    }
}