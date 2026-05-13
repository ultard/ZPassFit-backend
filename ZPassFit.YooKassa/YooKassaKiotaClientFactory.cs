using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Bundle;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Serialization.Json;

namespace ZPassFit.YooKassa;

/// <summary>
/// Builds <see cref="YooKassaApiClient"/> with YooKassa Basic Auth and Kiota <see cref="IRequestAdapter"/>.
/// </summary>
public static class YooKassaKiotaClientFactory
{
    /// <summary>
    /// Creates the Kiota API client using shop credentials from <paramref name="options"/>.
    /// </summary>
    public static YooKassaApiClient Create(YooKassaClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);
        var adapter = CreateRequestAdapter(options);
        return new YooKassaApiClient(adapter);
    }

    /// <summary>
    /// Creates a <see cref="IRequestAdapter"/> configured for YooKassa (for custom wiring or tests).
    /// </summary>
    public static IRequestAdapter CreateRequestAdapter(YooKassaClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        var auth = new YooKassaBasicAuthenticationProvider(options.ShopId, options.SecretKey);
        var httpClient = options.HttpClient ?? new HttpClient();

        var adapter = new DefaultRequestAdapter(
            auth,
            new JsonParseNodeFactory(),
            new JsonSerializationWriterFactory(),
            httpClient,
            new ObservabilityOptions())
        {
            BaseUrl = options.BaseUrl.TrimEnd('/')
        };

        return adapter;
    }

    private static void ValidateOptions(YooKassaClientOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ShopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SecretKey);
    }
}
