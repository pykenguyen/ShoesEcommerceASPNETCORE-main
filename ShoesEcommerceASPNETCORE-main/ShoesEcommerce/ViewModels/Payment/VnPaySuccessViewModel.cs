using ShoesEcommerce.Models.Orders;

namespace ShoesEcommerce.ViewModels.Payment
{
    /// <summary>
    /// ViewModel for VNPay success page containing all order and payment details
    /// </summary>
    public class VnPaySuccessViewModel
    {
        // Order info
        public int OrderId { get; set; }
        public DateTime OrderCreatedAt { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }

        // Payment info
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "VNPay";
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }

        // Invoice info
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime? InvoiceIssuedAt { get; set; }
        public InvoiceStatus InvoiceStatus { get; set; }

        // VNPay specific - Basic
        public string VnPayTxnRef { get; set; } = string.Empty;
        public string VnPayBankCode { get; set; } = string.Empty;
        public string VnPayResponseCode { get; set; } = string.Empty;

        // VNPay specific - Extended mapping
        /// <summary>VNPay Transaction Number (Mã GD) - e.g. 15385116</summary>
        public string VnPayTransactionNo { get; set; } = string.Empty;
        
        /// <summary>Bank Transaction Number (S? trace) - e.g. 4802564</summary>
        public string VnPayBankTranNo { get; set; } = string.Empty;
        
        /// <summary>Card Type - e.g. ATM, VISA, MASTERCARD</summary>
        public string VnPayCardType { get; set; } = string.Empty;
        
        /// <summary>Amount from VNPay (in VND * 100) - e.g. 100000000 for 1,000,000 VND</summary>
        public string VnPayAmount { get; set; } = string.Empty;
        
        /// <summary>Payment date from VNPay (format: yyyyMMddHHmmss)</summary>
        public string VnPayPayDate { get; set; } = string.Empty;
        
        /// <summary>Order Info sent to VNPay - e.g. "Thanh toan don hang 57"</summary>
        public string VnPayOrderInfo { get; set; } = string.Empty;

        // Computed properties for display
        /// <summary>Get formatted amount from VNPay (divide by 100)</summary>
        public decimal VnPayAmountFormatted => 
            !string.IsNullOrEmpty(VnPayAmount) && long.TryParse(VnPayAmount, out var amt) 
                ? amt / 100m 
                : 0;

        /// <summary>Get parsed payment date from VNPay</summary>
        public DateTime? VnPayPayDateParsed =>
            !string.IsNullOrEmpty(VnPayPayDate) && DateTime.TryParseExact(VnPayPayDate, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var dt)
                ? dt
                : null;

        // Customer info
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Shipping address
        public string ShippingFullName { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingDistrict { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;

        // Order items
        public List<OrderItemViewModel> Items { get; set; } = new();

        // Secure token for URL (hashed from orderId + transactionId)
        public string SecureToken { get; set; } = string.Empty;
    }

    public class OrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string VariantColor { get; set; } = string.Empty;
        public string VariantSize { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
