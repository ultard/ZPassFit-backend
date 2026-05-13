using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Payments;
using ZPassFit.Services.Interfaces;
using ZPassFit.YooKassa;
using ZPassFit.YooKassa.Models;
using ZPassFit.YooKassa.Payments;
using DomainPaymentMethod = ZPassFit.Data.Models.Memberships.PaymentMethod;
using DomainPaymentStatus = ZPassFit.Data.Models.Memberships.PaymentStatus;
using MembershipPayment = ZPassFit.Data.Models.Memberships.Payment;
using YkPayment = ZPassFit.YooKassa.Models.Payment;
using YkApiPaymentStatus = ZPassFit.YooKassa.Models.PaymentStatus;

namespace ZPassFit.Services.Implementations;

public class YooKassaService(
    YooKassaApiClient yooKassaClient,
    IOptions<YooKassaOptions> yooKassaOptions,
    IOptions<PaymentMethodsOptions> paymentMethodsOptions,
    IClientRepository clientRepository,
    IMembershipPlanRepository planRepository,
    IMembershipRepository membershipRepository,
    IPaymentRepository paymentRepository
) : IYooKassaService
{
    private readonly YooKassaOptions _yk = yooKassaOptions.Value;
    private readonly PaymentMethodsOptions _pm = paymentMethodsOptions.Value;

    public async Task<StartYooKassaCheckoutResponse> StartCheckoutAsync(
        string userId,
        StartYooKassaCheckoutRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        if (!_pm.YooKassaEnabled)
            throw new InvalidOperationException("YooKassa payments are disabled.");

        if (string.IsNullOrWhiteSpace(_yk.ShopId) || string.IsNullOrWhiteSpace(_yk.SecretKey))
            throw new InvalidOperationException("YooKassa is not configured (ShopId / SecretKey).");

        if (string.IsNullOrWhiteSpace(_yk.ReturnUrl))
            throw new InvalidOperationException("YooKassa ReturnUrl is not configured.");

        var client = await clientRepository.GetByUserIdAsync(userId)
                     ?? throw new InvalidOperationException("Client profile not found.");

        var plan = await planRepository.GetByIdAsync(request.PlanId)
                   ?? throw new InvalidOperationException("Membership plan not found.");

        if (plan.Durations.Length > 0 && !plan.Durations.Contains(request.DurationDays))
            throw new InvalidOperationException("Selected duration is not allowed for this plan.");

        var dbPayment = new MembershipPayment
        {
            Id = Guid.NewGuid(),
            Amount = plan.Price,
            Method = DomainPaymentMethod.YooKassa,
            Status = DomainPaymentStatus.Pending,
            ClientId = client.Id,
            EmployeeId = null
        };
        await paymentRepository.AddAsync(dbPayment);

        var metadata = new Metadata
        {
            AdditionalData =
            {
                ["internal_payment_id"] = dbPayment.Id.ToString("D"),
                ["plan_id"] = plan.Id.ToString("D"),
                ["duration_days"] = request.DurationDays.ToString(CultureInfo.InvariantCulture)
            }
        };

        var body = new PaymentsPostRequestBody
        {
            Amount = new MonetaryAmount
            {
                Currency = CurrencyCode.RUB,
                Value = FormatRubAmount(plan.Price)
            },
            Capture = true,
            Description = Truncate($"Абонемент «{plan.Name}», {request.DurationDays} дн.", 128),
            Metadata = metadata,
            MerchantCustomerId = client.Id.ToString("D"),
            Confirmation = new PaymentsPostRequestBody.PaymentsPostRequestBody_confirmation
            {
                ConfirmationDataRedirect = new ConfirmationDataRedirect
                {
                    Type = ConfirmationDataType.Redirect,
                    ReturnUrl = AppendQueryParam(
                        _yk.ReturnUrl,
                        "paymentId",
                        dbPayment.Id.ToString("D"))
                }
            }
        };

        if (!string.IsNullOrEmpty(clientIp))
            body.ClientIp = clientIp;

        var idempotenceKey = Guid.NewGuid().ToString();

        YkPayment? created;
        try
        {
            created = await yooKassaClient.Payments.PostAsync(body,
                cfg => cfg.AddIdempotenceKey(idempotenceKey),
                cancellationToken);
        }
        catch (Exception ex)
        {
            dbPayment.Status = DomainPaymentStatus.Cancelled;
            await paymentRepository.UpdateAsync(dbPayment);
            throw new InvalidOperationException($"YooKassa API error: {ex.Message}", ex);
        }

        if (created?.Id is null)
            throw new InvalidOperationException("YooKassa returned no payment id.");

        var confirmationUrl = created.Confirmation?.ConfirmationRedirect?.ConfirmationUrl;
        if (string.IsNullOrEmpty(confirmationUrl))
            throw new InvalidOperationException("YooKassa returned no confirmation URL.");

        dbPayment.YooKassaPaymentId = created.Id;
        await paymentRepository.UpdateAsync(dbPayment);

        return new StartYooKassaCheckoutResponse(dbPayment.Id, confirmationUrl, created.Id);
    }

    public async Task<SyncYooKassaPaymentResponse> SyncPaymentFromApiAsync(
        string userId,
        Guid internalPaymentId,
        CancellationToken cancellationToken = default)
    {
        if (!_pm.YooKassaEnabled)
            return new SyncYooKassaPaymentResponse("not_applicable", null, "Онлайн-оплата ЮKassa отключена.");

        if (string.IsNullOrWhiteSpace(_yk.ShopId) || string.IsNullOrWhiteSpace(_yk.SecretKey))
            return new SyncYooKassaPaymentResponse("not_applicable", null, "ЮKassa не настроена.");

        var client = await clientRepository.GetByUserIdAsync(userId);
        if (client is null)
            return new SyncYooKassaPaymentResponse("not_found", null, "Профиль клиента не найден.");

        var payment = await paymentRepository.GetByIdAsync(internalPaymentId);
        if (payment is null)
            return new SyncYooKassaPaymentResponse("not_found");

        if (payment.ClientId != client.Id)
            return new SyncYooKassaPaymentResponse("forbidden");

        if (payment.Method != DomainPaymentMethod.YooKassa)
            return new SyncYooKassaPaymentResponse("not_applicable", null, "Это не платёж ЮKassa.");

        if (payment.Status == DomainPaymentStatus.Completed)
            return new SyncYooKassaPaymentResponse("already_completed");

        if (payment.Status == DomainPaymentStatus.Cancelled)
            return new SyncYooKassaPaymentResponse("already_cancelled");

        if (string.IsNullOrEmpty(payment.YooKassaPaymentId))
            return new SyncYooKassaPaymentResponse("not_applicable", null, "Нет идентификатора платежа ЮKassa.");

        YkPayment? yk;
        try
        {
            yk = await yooKassaClient.Payments[payment.YooKassaPaymentId]
                .GetAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return new SyncYooKassaPaymentResponse("api_error", null, ex.Message);
        }

        if (yk is null)
            return new SyncYooKassaPaymentResponse("api_error", null, "Пустой ответ ЮKassa.");

        var statusLabel = yk.Status?.ToString();

        switch (yk.Status)
        {
            case YkApiPaymentStatus.Succeeded:
                if (!TryGetMetadataFromModel(yk.Metadata, "plan_id", out var planIdStr)
                    || !Guid.TryParse(planIdStr, out var planId))
                {
                    return new SyncYooKassaPaymentResponse(
                        "api_error",
                        statusLabel,
                        "В метаданных нет plan_id.");
                }

                if (!TryGetMetadataFromModel(yk.Metadata, "duration_days", out var durationStr)
                    || !int.TryParse(durationStr, CultureInfo.InvariantCulture, out var durationDays))
                {
                    return new SyncYooKassaPaymentResponse(
                        "api_error",
                        statusLabel,
                        "В метаданных нет duration_days.");
                }

                if (!ValidateAmountFromYk(yk, payment.Amount))
                    return new SyncYooKassaPaymentResponse("api_error", statusLabel, "Сумма не совпадает.");

                if (TryGetMetadataFromModel(yk.Metadata, "internal_payment_id", out var mid)
                    && Guid.TryParse(mid, out var metaPid)
                    && metaPid != payment.Id)
                {
                    return new SyncYooKassaPaymentResponse(
                        "api_error",
                        statusLabel,
                        "internal_payment_id не совпадает с записью.");
                }

                await ApplyMembershipPurchaseAsync(
                    payment,
                    planId,
                    durationDays,
                    yk.Id ?? payment.YooKassaPaymentId!,
                    cancellationToken);
                return new SyncYooKassaPaymentResponse("completed", statusLabel);

            case YkApiPaymentStatus.Canceled:
                payment.Status = DomainPaymentStatus.Cancelled;
                await paymentRepository.UpdateAsync(payment);
                return new SyncYooKassaPaymentResponse("cancelled", statusLabel);

            default:
                return new SyncYooKassaPaymentResponse("still_pending", statusLabel);
        }
    }

    public async Task HandleNotificationAsync(JsonElement root, CancellationToken cancellationToken = default)
    {
        if (!root.TryGetProperty("event", out var eventEl))
            return;

        var eventName = eventEl.GetString();
        if (!root.TryGetProperty("object", out var obj))
            return;

        switch (eventName)
        {
            case "payment.succeeded":
                await HandlePaymentSucceededAsync(obj, cancellationToken);
                break;
            case "payment.canceled":
                await HandlePaymentCanceledAsync(obj, cancellationToken);
                break;
        }
    }

    private async Task HandlePaymentSucceededAsync(JsonElement obj, CancellationToken cancellationToken)
    {
        if (!obj.TryGetProperty("id", out var idEl))
            return;
        var ykId = idEl.GetString();
        if (string.IsNullOrEmpty(ykId))
            return;

        MembershipPayment? payment = null;

        if (TryGetMetadataString(obj, "internal_payment_id", out var internalId)
            && Guid.TryParse(internalId, out var pid))
            payment = await paymentRepository.GetByIdAsync(pid);

        payment ??= await paymentRepository.GetByYooKassaPaymentIdAsync(ykId, cancellationToken);

        if (payment is null || payment.Status != DomainPaymentStatus.Pending)
            return;

        if (payment.Method != DomainPaymentMethod.YooKassa)
            return;

        if (!TryGetMetadataString(obj, "plan_id", out var planIdStr) || !Guid.TryParse(planIdStr, out var planId))
            return;

        if (!TryGetMetadataString(obj, "duration_days", out var durationStr)
            || !int.TryParse(durationStr, CultureInfo.InvariantCulture, out var durationDays))
            return;

        if (!ValidateAmount(obj, payment.Amount))
            return;

        await ApplyMembershipPurchaseAsync(payment, planId, durationDays, ykId, cancellationToken);
    }

    private async Task ApplyMembershipPurchaseAsync(
        MembershipPayment payment,
        Guid planId,
        int durationDays,
        string ykId,
        CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(payment.ClientId)
                     ?? throw new InvalidOperationException("Client not found.");

        var plan = await planRepository.GetByIdAsync(planId)
                   ?? throw new InvalidOperationException("Plan not found.");

        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;
        var membership = await membershipRepository.GetByClientIdAsync(client.Id);

        if (membership == null)
        {
            membership = new Membership
            {
                ClientId = client.Id,
                PlanId = plan.Id,
                Status = MembershipStatus.Active,
                AutoRenewEnabled = true,
                ActivatedDate = now,
                ExpireDate = now.AddDays(durationDays)
            };
            await membershipRepository.AddAsync(membership);
        }
        else
        {
            membership.PlanId = plan.Id;
            membership.Status = MembershipStatus.Active;
            membership.AutoRenewEnabled = true;
            membership.ActivatedDate = now;
            membership.ExpireDate = now.AddDays(durationDays);
            await membershipRepository.UpdateAsync(membership);
        }

        payment.Status = DomainPaymentStatus.Completed;
        payment.PaymentDate = now;
        payment.YooKassaPaymentId ??= ykId;
        await paymentRepository.UpdateAsync(payment);
    }

    private async Task HandlePaymentCanceledAsync(JsonElement obj, CancellationToken cancellationToken)
    {
        if (!obj.TryGetProperty("id", out var idEl))
            return;
        var ykId = idEl.GetString();
        if (string.IsNullOrEmpty(ykId))
            return;

        MembershipPayment? payment = null;
        if (TryGetMetadataString(obj, "internal_payment_id", out var internalId)
            && Guid.TryParse(internalId, out var pid))
            payment = await paymentRepository.GetByIdAsync(pid);

        payment ??= await paymentRepository.GetByYooKassaPaymentIdAsync(ykId, cancellationToken);

        if (payment is null || payment.Status != DomainPaymentStatus.Pending || payment.Method != DomainPaymentMethod.YooKassa)
            return;

        payment.Status = DomainPaymentStatus.Cancelled;
        await paymentRepository.UpdateAsync(payment);
    }

    private static bool ValidateAmount(JsonElement paymentObject, int expectedAmountRub)
    {
        if (!paymentObject.TryGetProperty("amount", out var amountObj))
            return true;

        if (!amountObj.TryGetProperty("value", out var valueEl))
            return true;

        var valueStr = valueEl.GetString();
        if (string.IsNullOrEmpty(valueStr))
            return true;

        if (!decimal.TryParse(valueStr, CultureInfo.InvariantCulture, out var paid))
            return false;

        var expected = (decimal)expectedAmountRub;
        return paid == expected;
    }

    private static bool ValidateAmountFromYk(YkPayment yk, int expectedAmountRub)
    {
        var valueStr = yk.Amount?.Value;
        if (string.IsNullOrEmpty(valueStr))
            return true;

        if (!decimal.TryParse(valueStr, CultureInfo.InvariantCulture, out var paid))
            return false;

        return paid == expectedAmountRub;
    }

    private static bool TryGetMetadataFromModel(Metadata? metadata, string key, out string value)
    {
        value = "";
        if (metadata?.AdditionalData is null ||
            !metadata.AdditionalData.TryGetValue(key, out var raw) ||
            raw is null)
            return false;

        value = MetadataValueToString(raw);
        return !string.IsNullOrEmpty(value);
    }

    private static string MetadataValueToString(object raw) =>
        raw switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } je => je.GetString() ?? "",
            JsonElement { ValueKind: JsonValueKind.Number } je => je.GetRawText(),
            JsonElement je => je.ToString(),
            _ => raw.ToString() ?? ""
        };

    private static bool TryGetMetadataString(JsonElement obj, string key, out string value)
    {
        value = "";
        if (!obj.TryGetProperty("metadata", out var meta) || meta.ValueKind != JsonValueKind.Object)
            return false;

        if (!meta.TryGetProperty(key, out var prop))
            return false;

        value = prop.GetString() ?? "";
        return !string.IsNullOrEmpty(value);
    }

    private static string FormatRubAmount(int rubles) =>
        rubles.ToString("F2", CultureInfo.InvariantCulture);

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen];

    private static string AppendQueryParam(string url, string key, string value)
    {
        var sep = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{sep}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }
}
