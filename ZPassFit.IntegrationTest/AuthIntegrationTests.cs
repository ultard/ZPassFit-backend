using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ZPassFit.Dto;
using ZPassFit.IntegrationTest.Infrastructure;

namespace ZPassFit.IntegrationTest;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthIntegrationTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task Login_ThenGetProfile_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await _client.LoginAsync("client@dev.local", "DevPassword123!", ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/client/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ClientResponse>(cancellationToken: ct);
        Assert.NotNull(profile);
        Assert.Equal("client@dev.local", profile.Email);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/client/profile", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAudit_AsClient_ReturnsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var token = await _client.LoginAsync("client@dev.local", "DevPassword123!", ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/audit");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
