namespace ZPassFit.Options.Payments;

public class PaymentMethodsOptions
{
    public const string SectionName = "PaymentMethods";

    public bool CashEnabled { get; set; } = true;
    public bool CardEnabled { get; set; } = true;
    public bool BalanceEnabled { get; set; } = true;
    public bool YooKassaEnabled { get; set; }
}