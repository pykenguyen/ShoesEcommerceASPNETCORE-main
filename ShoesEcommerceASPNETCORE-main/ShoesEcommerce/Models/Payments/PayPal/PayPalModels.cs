namespace ShoesEcommerce.Models.Payments.PayPal
{
    // ========== AUTHENTICATION ==========
    public sealed class AuthResponse
    {
        public string scope { get; set; } = string.Empty;
        public string access_token { get; set; } = string.Empty;
        public string token_type { get; set; } = string.Empty;
        public string app_id { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string nonce { get; set; } = string.Empty;
    }

    // ========== CREATE ORDER ==========
    public sealed class CreateOrderRequest
    {
        public string intent { get; set; } = "CAPTURE";
        public List<PurchaseUnit> purchase_units { get; set; } = new();
        public ApplicationContext? application_context { get; set; }
    }

    public sealed class ApplicationContext
    {
        public string brand_name { get; set; } = "ShoesEcommerce";
        public string landing_page { get; set; } = "BILLING";
        public string user_action { get; set; } = "PAY_NOW";
        public string return_url { get; set; } = string.Empty;
        public string cancel_url { get; set; } = string.Empty;
    }

    public sealed class CreateOrderResponse
    {
        public string id { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public List<Link> links { get; set; } = new();
    }

    // ========== CAPTURE ORDER ==========
    public sealed class CaptureOrderResponse
    {
        public string id { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public PaymentSource? payment_source { get; set; }
        public List<PurchaseUnit> purchase_units { get; set; } = new();
        public Payer? payer { get; set; }
        public List<Link> links { get; set; } = new();
    }

    // ========== COMMON MODELS ==========
    public sealed class PurchaseUnit
    {
        public Amount? amount { get; set; }
        public string reference_id { get; set; } = string.Empty;
        public Shipping? shipping { get; set; }
        public Payments? payments { get; set; }
        public string? description { get; set; }
        public string? invoice_id { get; set; }
        public string? custom_id { get; set; }
        public List<Item>? items { get; set; }
    }

    /// <summary>
    /// PayPal Item - represents a product in the order
    /// </summary>
    public sealed class Item
    {
        public string name { get; set; } = string.Empty;
        public string quantity { get; set; } = "1";
        public string? description { get; set; }
        public string? sku { get; set; }
        public UnitAmount unit_amount { get; set; } = new();
        public string? category { get; set; } = "PHYSICAL_GOODS"; // PHYSICAL_GOODS, DIGITAL_GOODS, DONATION
    }

    public sealed class UnitAmount
    {
        public string currency_code { get; set; } = "USD";
        public string value { get; set; } = "0.00";
    }

    public sealed class Amount
    {
        public string currency_code { get; set; } = "USD";
        public string value { get; set; } = "0.00";
        public Breakdown? breakdown { get; set; }
    }

    public sealed class Breakdown
    {
        public Amount? item_total { get; set; }
        public Amount? shipping { get; set; }
        public Amount? discount { get; set; }
        public Amount? tax { get; set; }
    }

    public sealed class Shipping
    {
        public Address? address { get; set; }
        public Name? name { get; set; }
    }

    public sealed class Address
    {
        public string address_line_1 { get; set; } = string.Empty;
        public string address_line_2 { get; set; } = string.Empty;
        public string admin_area_2 { get; set; } = string.Empty;
        public string admin_area_1 { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string country_code { get; set; } = "VN";
    }

    public sealed class Name
    {
        public string given_name { get; set; } = string.Empty;
        public string surname { get; set; } = string.Empty;
        public string full_name { get; set; } = string.Empty;
    }

    public sealed class Payments
    {
        public List<Capture> captures { get; set; } = new();
    }

    public sealed class Capture
    {
        public string id { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public Amount? amount { get; set; }
        public SellerProtection? seller_protection { get; set; }
        public bool final_capture { get; set; }
        public string disbursement_mode { get; set; } = string.Empty;
        public SellerReceivableBreakdown? seller_receivable_breakdown { get; set; }
        public DateTime create_time { get; set; }
        public DateTime update_time { get; set; }
        public List<Link> links { get; set; } = new();
    }

    public sealed class SellerProtection
    {
        public string status { get; set; } = string.Empty;
        public List<string> dispute_categories { get; set; } = new();
    }

    public sealed class SellerReceivableBreakdown
    {
        public Amount? gross_amount { get; set; }
        public PaypalFee? paypal_fee { get; set; }
        public Amount? net_amount { get; set; }
    }

    public sealed class PaypalFee
    {
        public string currency_code { get; set; } = "USD";
        public string value { get; set; } = "0.00";
    }

    public sealed class PaymentSource
    {
        public Paypal? paypal { get; set; }
    }

    public sealed class Paypal
    {
        public Name? name { get; set; }
        public string email_address { get; set; } = string.Empty;
        public string account_id { get; set; } = string.Empty;
    }

    public sealed class Payer
    {
        public Name? name { get; set; }
        public string email_address { get; set; } = string.Empty;
        public string payer_id { get; set; } = string.Empty;
        public Address? address { get; set; }
    }

    public sealed class Link
    {
        public string href { get; set; } = string.Empty;
        public string rel { get; set; } = string.Empty;
        public string method { get; set; } = string.Empty;
    }
}
