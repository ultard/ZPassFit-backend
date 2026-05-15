using System.Net;
using System.Net.Http.Json;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Dto;
using ZPassFit.IntegrationTest.Infrastructure;

namespace ZPassFit.IntegrationTest.BusinessProcess;

/// <summary>
/// БП 1: Покупка и активация абонемента.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class MembershipPurchaseProcessTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task PurchaseWithBalance_ActivatesMembership_RecordsPayment_DecreasesBalance_WritesAudit()
    {
        var token = await _client.LoginAsync("client3@dev.local", "DevPassword123!");
        var adminToken = await _client.LoginAsync("admin@dev.local", "DevPassword123!");

        var profileBefore = await _client.GetAuthenticatedAsync(token, "/client/profile");
        profileBefore.EnsureSuccessStatusCode();
        var profile = await profileBefore.Content.ReadFromJsonAsync<ClientResponse>();
        Assert.NotNull(profile);

        var plansResponse = await _client.GetAsync("/membership/plans");
        plansResponse.EnsureSuccessStatusCode();
        var plans = await plansResponse.Content.ReadFromJsonAsync<List<MembershipPlanResponse>>();
        Assert.NotNull(plans);
        Assert.NotEmpty(plans);

        var plan = plans[0];
        var duration = plan.Durations.Length > 0 ? plan.Durations[0] : 30;

        var buyResponse = await _client.PostAuthenticatedJsonAsync(
            token,
            "/membership/buy",
            new BuyMembershipRequest(plan.Id, duration, PaymentMethod.Balance)
        );
        Assert.Equal(HttpStatusCode.OK, buyResponse.StatusCode);

        var membership = await buyResponse.Content.ReadFromJsonAsync<MembershipResponse>();
        Assert.NotNull(membership);
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Equal(plan.Id, membership.PlanId);

        var paymentsResponse = await _client.GetAuthenticatedAsync(token, "/client/payments");
        paymentsResponse.EnsureSuccessStatusCode();
        var payments = await paymentsResponse.Content.ReadFromJsonAsync<List<PaymentResponse>>();
        Assert.NotNull(payments);
        Assert.Contains(payments, p => p.Method == PaymentMethod.Balance && p.Status == PaymentStatus.Completed);

        var profileAfter = await _client.GetAuthenticatedAsync(token, "/client/profile");
        profileAfter.EnsureSuccessStatusCode();
        var profileUpdated = await profileAfter.Content.ReadFromJsonAsync<ClientResponse>();
        Assert.NotNull(profileUpdated);
        Assert.True(profileUpdated.Balance < profile.Balance);

        var auditResponse = await _client.GetAuthenticatedAsync(
            adminToken,
            "/audit?entityType=Payment&action=Insert&pageSize=5"
        );
        auditResponse.EnsureSuccessStatusCode();
        var audit = await auditResponse.Content.ReadFromJsonAsync<PagedAuditLogsResponse>();
        Assert.NotNull(audit);
        Assert.Contains(audit.Items, i => i.EntityType.Contains("Payment", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PurchaseWithInvalidDuration_ReturnsBadRequest()
    {
        var token = await _client.LoginAsync("client2@dev.local", "DevPassword123!");

        var plansResponse = await _client.GetAsync("/membership/plans");
        plansResponse.EnsureSuccessStatusCode();
        var plans = await plansResponse.Content.ReadFromJsonAsync<List<MembershipPlanResponse>>();
        Assert.NotNull(plans);

        var planWithDurations = plans.First(p => p.Durations.Length > 0);
        var invalidDuration = planWithDurations.Durations.Max() + 17;

        var buyResponse = await _client.PostAuthenticatedJsonAsync(
            token,
            "/membership/buy",
            new BuyMembershipRequest(
                planWithDurations.Id,
                invalidDuration,
                PaymentMethod.Balance
            )
        );

        Assert.Equal(HttpStatusCode.BadRequest, buyResponse.StatusCode);
    }
}
