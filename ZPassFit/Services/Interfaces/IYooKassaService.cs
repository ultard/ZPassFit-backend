using System.Text.Json;
using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IYooKassaService
{
    Task<StartYooKassaCheckoutResponse> StartCheckoutAsync(
        string userId,
        StartYooKassaCheckoutRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default);

    Task HandleNotificationAsync(JsonElement root, CancellationToken cancellationToken = default);

    /// <summary>
    /// Запросить актуальный статус платежа в ЮKassa и при необходимости завершить покупку (дублирует обработку webhook).
    /// </summary>
    Task<SyncYooKassaPaymentResponse> SyncPaymentFromApiAsync(
        string userId,
        Guid internalPaymentId,
        CancellationToken cancellationToken = default);
}
