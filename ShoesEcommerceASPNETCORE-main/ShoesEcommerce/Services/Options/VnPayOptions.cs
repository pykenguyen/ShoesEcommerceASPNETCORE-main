namespace ShoesEcommerce.Services.Options
{
    public class VnPayOptions
    {
        public const string SectionName = "VNPay";

        public string? TmnCode { get; set; }
        public string? HashSecret { get; set; }
        public string Url { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public string? ReturnUrl { get; set; }
    }
}
