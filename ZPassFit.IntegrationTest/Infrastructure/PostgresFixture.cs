using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using ZPassFit.Data;
using ZPassFit.Data.Dev;

namespace ZPassFit.IntegrationTest.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public HttpClient Client { get; private set; } = null!;

    public PostgresWebApplicationFactory Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ZPASSFIT_TEST_DB")
            ?? (
                Environment.GetEnvironmentVariable("ZPASSFIT_USE_TESTCONTAINERS") == "true"
                    ? null
                    : "Host=localhost;Database=zpassfit;Username=postgres;Password=postgres"
            );

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _container = new PostgreSqlBuilder("postgres:alpine").Build();
            await _container.StartAsync();
            connectionString = _container.GetConnectionString();
        }

        Factory = new PostgresWebApplicationFactory(connectionString);
        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await DevelopmentSeed.EnsureSeededAsync(scope.ServiceProvider);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
