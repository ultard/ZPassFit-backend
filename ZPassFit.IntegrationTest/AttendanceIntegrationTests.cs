using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ZPassFit.Dto;
using ZPassFit.IntegrationTest.Infrastructure;

namespace ZPassFit.IntegrationTest;

[Collection(IntegrationTestCollection.Name)]
public sealed class AttendanceIntegrationTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task QrSession_ThenCheckIn_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var clientToken = await _client.LoginAsync("client@dev.local", "DevPassword123!", ct);
        var adminToken = await _client.LoginAsync("admin@dev.local", "DevPassword123!", ct);

        using var qrRequest = new HttpRequestMessage(HttpMethod.Post, "/attendance/qr_session");
        qrRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

        var qrResponse = await _client.SendAsync(qrRequest, ct);
        Assert.Equal(HttpStatusCode.OK, qrResponse.StatusCode);

        var session =
            await qrResponse.Content.ReadFromJsonAsync<QrSessionResponse>(cancellationToken: ct);
        Assert.NotNull(session);

        using var checkinRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/attendance/checkin/{session.Token}"
        );
        checkinRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var checkinResponse = await _client.SendAsync(checkinRequest, ct);

        Assert.Equal(HttpStatusCode.OK, checkinResponse.StatusCode);
    }
}
