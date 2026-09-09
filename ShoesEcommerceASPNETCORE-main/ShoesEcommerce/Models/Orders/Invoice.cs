namespace ShoesEcommerce.Models.Orders
{
    /// <summary>
    /// Invoice status enum for tracking payment lifecycle
    /// </summary>
    public enum InvoiceStatus
    {
        /// <summary>Invoice created but payment not initiated</summary>
        Draft = 0,
        
        /// <summary>Payment initiated, waiting for completion</summary>
        Pending = 1,
        
        /// <summary>Payment completed successfully</summary>
        Paid = 2,
        
        /// <summary>Payment failed or cancelled by user</summary>
        Cancelled = 3,
        
        /// <summary>Payment was refunded</summary>
        Refunded = 4,
        
        /// <summary>Payment partially refunded</summary>
        PartiallyRefunded = 5
    }

    public class Invoice
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public decimal Amount { get; set; }

        // ✅ NEW: Status tracking for transaction safety
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        // ✅ NEW: Payment gateway transaction references
        public string? PayPalOrderId { get; set; }
        public string? PayPalTransactionId { get; set; }
        public string? VnPayTransactionId { get; set; }
        public string? VnPayTxnRef { get; set; }
        public string? VnPayBankCode { get; set; }
        public string? VnPayBankTranNo { get; set; }
        public string? VnPayCardType { get; set; }

        // ✅ NEW: Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        // ✅ NEW: Refund tracking
        public decimal? RefundedAmount { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? RefundTransactionId { get; set; }

        // ✅ NEW: Currency info (for international payments)
        public string Currency { get; set; } = "VND";
        public decimal? AmountInUSD { get; set; }
        public decimal? ExchangeRate { get; set; }
    }
}
