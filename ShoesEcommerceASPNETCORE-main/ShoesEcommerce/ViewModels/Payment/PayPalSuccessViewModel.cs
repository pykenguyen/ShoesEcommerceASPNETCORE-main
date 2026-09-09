using ShoesEcommerce.Models.Orders;

namespace ShoesEcommerce.ViewModels.Payment
{
    /// <summary>
    /// ViewModel for PayPal success page containing all order and payment details
    /// </summary>
    public class PayPalSuccessViewModel
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
        public string PaymentMethod { get; set; } = "PayPal";
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }

        // Invoice info
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime? InvoiceIssuedAt { get; set; }
        public InvoiceStatus InvoiceStatus { get; set; }

        // PayPal specific
        /// <summary>PayPal Order ID (e.g., 2AB12345CD678901E)</summary>
        public string PayPalOrderId { get; set; } = string.Empty;
        
        /// <summary>PayPal Transaction/Capture ID</summary>
        public string PayPalTransactionId { get; set; } = string.Empty;
        
        /// <summary>PayPal Payer Email</summary>
        public string PayPalPayerEmail { get; set; } = string.Empty;
        
        /// <summary>PayPal Payer Name</summary>
        public string PayPalPayerName { get; set; } = string.Empty;
        
        /// <summary>Amount in USD sent to PayPal</summary>
        public decimal? AmountInUSD { get; set; }
        
        /// <summary>Exchange rate used</summary>
        public decimal? ExchangeRate { get; set; }
        
        /// <summary>PayPal token from redirect</summary>
        public string PayPalToken { get; set; } = string.Empty;

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
    }
}
