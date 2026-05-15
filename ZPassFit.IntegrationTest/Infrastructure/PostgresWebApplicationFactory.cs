using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.IntegrationTest.Infrastructure;

public sealed class PostgresWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                }
            );
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPredictionService>();
            services.AddScoped<IPredictionService, StubPredictionService>();
        });
    }
}
