using Microsoft.Extensions.DependencyInjection;

namespace ZPassFit.YooKassa;

/// <summary>Registers the Kiota <see cref="YooKassaApiClient"/> with dependency injection.</summary>
public static class YooKassaServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="YooKassaApiClient"/> as a singleton. Configure shop id, secret key, and optionally <see cref="YooKassaClientOptions.HttpClient"/>.
    /// </summary>
    public static IServiceCollection AddYooKassaApiClient(
        this IServiceCollection services,
        Action<YooKassaClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton(_ =>
        {
            var options = new YooKassaClientOptions();
            configure(options);
            return YooKassaKiotaClientFactory.Create(options);
        });

        return services;
    }
}
