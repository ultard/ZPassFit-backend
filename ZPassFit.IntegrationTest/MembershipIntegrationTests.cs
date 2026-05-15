using System.Net;
using System.Net.Http.Json;
using ZPassFit.IntegrationTest.Infrastructure;

namespace ZPassFit.IntegrationTest;

[Collection(IntegrationTestCollection.Name)]
public sealed class MembershipIntegrationTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task GetPlans_ReturnsNonEmptyList()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/membership/plans", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plans =
            await response.Content.ReadFromJsonAsync<List<MembershipPlanItem>>(cancellationToken: ct);
        Assert.NotNull(plans);
        Assert.NotEmpty(plans);
    }

    private sealed record MembershipPlanItem(Guid Id, string Name);
}
