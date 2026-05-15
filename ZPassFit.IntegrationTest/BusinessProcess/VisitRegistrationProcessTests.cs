using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZPassFit.Attendance;
using ZPassFit.Data;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Dto;
using ZPassFit.IntegrationTest.Infrastructure;

namespace ZPassFit.IntegrationTest.BusinessProcess;

/// <summary>
/// БП 2: Регистрация посещения (QR → check-in → check-out).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class VisitRegistrationProcessTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task QrCheckInAndCheckOut_UpdatesVisitHistory_WritesAudit()
    {
        var ct = TestContext.Current.CancellationToken;
        var clientToken = await _client.LoginAsync("client@dev.local", "DevPassword123!", ct);
        var adminToken = await _client.LoginAsync("admin@dev.local", "DevPassword123!", ct);

        var qrResponse = await _client.PostAuthenticatedJsonAsync<object?>(
            clientToken,
            "/attendance/qr_session",
            null,
            ct
        );
        Assert.Equal(HttpStatusCode.OK, qrResponse.StatusCode);
        var session = await qrResponse.Content.ReadFromJsonAsync<QrSessionResponse>(cancellationToken: ct);
        Assert.NotNull(session);

        var checkinResponse = await _client.SendAuthenticatedAsync(
            adminToken,
            HttpMethod.Post,
            $"/attendance/checkin/{session.Token}",
            cancellationToken: ct
        );
        Assert.Equal(HttpStatusCode.OK, checkinResponse.StatusCode);
        var visit = await checkinResponse.Content.ReadFromJsonAsync<VisitLogResponse>(cancellationToken: ct);
        Assert.NotNull(visit);
        Assert.Null(visit.LeaveDate);

        var checkoutResponse = await _client.PostAuthenticatedJsonAsync<object?>(
            clientToken,
            "/attendance/checkout",
            null,
            ct
        );
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);
        var closed = await checkoutResponse.Content.ReadFromJsonAsync<VisitLogResponse>(cancellationToken: ct);
        Assert.NotNull(closed);
        Assert.NotNull(closed.LeaveDate);

        var historyResponse = await _client.GetAuthenticatedAsync(
            clientToken,
            "/attendance/visits/history",
            ct
        );
        historyResponse.EnsureSuccessStatusCode();
        var history = await historyResponse.Content.ReadFromJsonAsync<List<VisitLogResponse>>(cancellationToken: ct);
        Assert.NotNull(history);
        Assert.Contains(history, v => v.Id == visit.Id && v.LeaveDate != null);

        var auditResponse = await _client.GetAuthenticatedAsync(
            adminToken,
            "/audit?entityType=VisitLog&action=Insert&pageSize=10",
            ct
        );
        auditResponse.EnsureSuccessStatusCode();
        var audit = await auditResponse.Content.ReadFromJsonAsync<PagedAuditLogsResponse>(cancellationToken: ct);
        Assert.NotNull(audit);
        Assert.Contains(audit.Items, i => i.EntityType.Contains("VisitLog", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CheckOut_AccruesDisciplineBonus_AndWritesBonusAudit()
    {
        var ct = TestContext.Current.CancellationToken;
        var clientToken = await _client.LoginAsync("client2@dev.local", "DevPassword123!", ct);
        var adminToken = await _client.LoginAsync("admin@dev.local", "DevPassword123!", ct);

        int bonusesBefore;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var client = await db.Clients.SingleAsync(c => c.Email == "client2@dev.local", ct);
            bonusesBefore = client.Bonuses;
        }

        var qrResponse = await _client.PostAuthenticatedJsonAsync<object?>(
            clientToken,
            "/attendance/qr_session",
            null,
            ct
        );
        qrResponse.EnsureSuccessStatusCode();
        var session = await qrResponse.Content.ReadFromJsonAsync<QrSessionResponse>(cancellationToken: ct);
        Assert.NotNull(session);

        var checkinResponse = await _client.SendAuthenticatedAsync(
            adminToken,
            HttpMethod.Post,
            $"/attendance/checkin/{session.Token}",
            cancellationToken: ct
        );
        checkinResponse.EnsureSuccessStatusCode();

        var checkoutResponse = await _client.PostAuthenticatedJsonAsync<object?>(
            clientToken,
            "/attendance/checkout",
            null,
            ct
        );
        checkoutResponse.EnsureSuccessStatusCode();
        var closed = await checkoutResponse.Content.ReadFromJsonAsync<VisitLogResponse>(cancellationToken: ct);
        Assert.NotNull(closed);

        int expectedBonus;
        using (var optionsScope = fixture.Factory.Services.CreateScope())
        {
            expectedBonus = optionsScope.ServiceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<AttendanceBonusOptions>>()
                .Value.DisciplineBonusPoints;
        }

        Assert.Equal(expectedBonus, closed.DisciplineBonusAccrued);

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var client = await db.Clients.SingleAsync(c => c.Email == "client2@dev.local", ct);
            Assert.Equal(bonusesBefore + expectedBonus, client.Bonuses);

            var accrual = await db.BonusTransactions.SingleAsync(
                t => t.VisitLogId == closed.Id && t.Type == ZPassFit.Data.Models.Clients.BonusTransactionType.Accrual,
                ct
            );
            Assert.Equal(expectedBonus, accrual.Amount);
        }
    }

    [Fact]
    public async Task CheckIn_WithoutMembership_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        Guid token;
        Membership? removedMembership = null;
        try
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var client = await db.Clients.SingleAsync(c => c.Email == "client2@dev.local", ct);
                var membership = await db.Memberships.SingleOrDefaultAsync(m => m.ClientId == client.Id, ct);
                if (membership is not null)
                {
                    removedMembership = membership;
                    db.Memberships.Remove(membership);
                    await db.SaveChangesAsync(ct);
                }

                token = Guid.NewGuid();
                db.QrSessions.Add(
                    new ZPassFit.Data.Models.Attendance.QrSession
                    {
                        Token = token,
                        ClientId = client.Id,
                        CreateDate = DateTime.UtcNow,
                        ExpireDate = DateTime.UtcNow.AddMinutes(5)
                    }
                );
                await db.SaveChangesAsync(ct);
            }

            var adminToken = await _client.LoginAsync("admin@dev.local", "DevPassword123!", ct);
            var checkinResponse = await _client.SendAuthenticatedAsync(
                adminToken,
                HttpMethod.Post,
                $"/attendance/checkin/{token}",
                cancellationToken: ct
            );

            Assert.Equal(HttpStatusCode.BadRequest, checkinResponse.StatusCode);
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
                    db.Memberships.Add(new Membership
                    {
                        ClientId = removedMembership.ClientId,
                        PlanId = removedMembership.PlanId,
                        Status = removedMembership.Status,
                        ActivatedDate = removedMembership.ActivatedDate,
                        ExpireDate = removedMembership.ExpireDate,
                        AutoRenewEnabled = removedMembership.AutoRenewEnabled
                    });
                    await db.SaveChangesAsync(ct);
                }
            }
        }
    }
}
