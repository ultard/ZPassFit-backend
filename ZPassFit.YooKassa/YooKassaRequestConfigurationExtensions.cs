using Microsoft.Kiota.Abstractions;

namespace ZPassFit.YooKassa;

/// <summary>
///     Helpers for YooKassa-specific HTTP headers on Kiota requests.
/// </summary>
public static class YooKassaRequestConfigurationExtensions
{
    /// <summary>Name of the idempotency header required for mutating YooKassa API calls.</summary>
    public const string IdempotenceKeyHeaderName = "Idempotence-Key";

    /// <summary>Adds <see cref="IdempotenceKeyHeaderName" /> (required for POST that create or change state).</summary>
    public static void AddIdempotenceKey<T>(this RequestConfiguration<T> configuration, string idempotenceKey)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotenceKey);
        configuration.Headers.TryAdd(IdempotenceKeyHeaderName, idempotenceKey);
    }
}