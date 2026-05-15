using System.Net;
using System.Net.Http.Headers;
using ZPassFit.IntegrationTest.Infrastructure;

namespace ZPassFit.IntegrationTest;

[Collection(IntegrationTestCollection.Name)]
public sealed class DashboardIntegrationTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task ListClients_AsAdmin_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await _client.LoginAsync("admin@dev.local", "DevPassword123!", ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard/clients");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
