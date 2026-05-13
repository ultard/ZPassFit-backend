namespace ZPassFit.YooKassa;

/// <summary>
/// Credentials and HTTP settings for the Kiota-generated YooKassa API client.
/// </summary>
public sealed class YooKassaClientOptions
{
    /// <summary>Shop ID (идентификатор магазина) from the YooKassa merchant profile.</summary>
    public string ShopId { get; init; } = "";

    /// <summary>Secret key from the YooKassa merchant profile.</summary>
    public string SecretKey { get; init; } = "";

    /// <summary>
    /// API base URL. Defaults to <c>https://api.yookassa.ru/v3</c>.
    /// </summary>
    public string BaseUrl { get; init; } = "https://api.yookassa.ru/v3";

    /// <summary>
    /// Optional shared <see cref="HttpClient"/> (for example from <c>IHttpClientFactory</c>).
    /// If omitted, the factory creates a new instance (caller should prefer injecting a shared client in production).
    /// </summary>
    public HttpClient? HttpClient { get; init; }
}
