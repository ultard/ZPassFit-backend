namespace ZPassFit.Payments;

public class YooKassaOptions
{
    public const string SectionName = "YooKassa";

    public string ShopId { get; set; } = "";
    public string SecretKey { get; set; } = "";
    
    public string BaseUrl { get; set; } = "https://api.yookassa.ru/v3";
    public string ReturnUrl { get; set; } = "";
}
