using ShoesEcommerce.Models.Orders;

namespace ShoesEcommerce.Repositories.Interfaces
{
    /// <summary>
    /// Interface for Payment repository operations
    /// </summary>
    public interface IPaymentRepository
    {
        /// <summary>
        /// Get payment by order ID
        /// </summary>
        Task<Payment?> GetByOrderIdAsync(int orderId);

        /// <summary>
        /// Get payment by payment ID
        /// </summary>
        Task<Payment?> GetByIdAsync(int id);

        /// <summary>
        /// Create a new payment record
        /// </summary>
        Task<Payment> CreateAsync(Payment payment);

        /// <summary>
        /// Update an existing payment
        /// </summary>
        Task<bool> UpdateAsync(Payment payment);

        /// <summary>
        /// Update payment status
        /// </summary>
        Task<bool> UpdateStatusAsync(int orderId, string status, DateTime? paidAt = null, string? transactionId = null);

        /// <summary>
        /// Get order with all details (for success/cancel pages)
        /// </summary>
        Task<Order?> GetOrderWithDetailsAsync(int orderId);

        /// <summary>
        /// Get order with items for PayPal (includes product names)
        /// </summary>
        Task<Order?> GetOrderWithItemsForPaymentAsync(int orderId);

        /// <summary>
        /// Check if order exists
        /// </summary>
        Task<bool> OrderExistsAsync(int orderId);

        /// <summary>
        /// Check if order belongs to customer
        /// </summary>
        Task<bool> OrderBelongsToCustomerAsync(int orderId, int customerId);

        /// <summary>
        /// Get invoice by order ID
        /// </summary>
        Task<Invoice?> GetInvoiceByOrderIdAsync(int orderId);

        /// <summary>
        /// Create or update invoice for an order
        /// </summary>
        Task<Invoice> CreateOrUpdateInvoiceAsync(int orderId, string invoiceNumber, decimal amount);

        /// <summary>
        /// Update invoice with payment transaction details
        /// </summary>
        Task<bool> UpdateInvoiceOnPaymentAsync(int orderId, string transactionId, DateTime paidAt);

        /// <summary>
        /// Update invoice status
        /// </summary>
        Task<bool> UpdateInvoiceStatusAsync(int orderId, InvoiceStatus status);

        /// <summary>
        /// Set PayPal Order ID on invoice for tracking
        /// </summary>
        Task<bool> SetInvoicePayPalOrderIdAsync(int orderId, string paypalOrderId);

        /// <summary>
        /// Cancel invoice with reason
        /// </summary>
        Task<bool> CancelInvoiceAsync(int orderId, string reason);

        /// <summary>
        /// Finalize invoice after successful payment
        /// </summary>
        Task<bool> FinalizeInvoiceAsync(int orderId, string transactionId, DateTime paidAt);

        /// <summary>
        /// Update order status
        /// </summary>
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);

        /// <summary>
        /// Get invoice by PayPal Order ID
        /// </summary>
        Task<Invoice?> GetInvoiceByPayPalOrderIdAsync(string paypalOrderId);

        /// <summary>
        /// Set VNPay Transaction ID on invoice for tracking
        /// </summary>
        Task<bool> SetInvoiceVnPayTransactionIdAsync(int orderId, string vnpayTransactionId);

        /// <summary>
        /// Finalize VNPay payment - update invoice with VNPay-specific data
        /// </summary>
        Task<bool> FinalizeVnPayInvoiceAsync(int orderId, string transactionId, string? bankCode, DateTime paidAt);

        /// <summary>
        /// Finalize VNPay payment with full data - update invoice with all VNPay response fields
        /// </summary>
        Task<bool> FinalizeVnPayInvoiceFullAsync(
            int orderId,
            string transactionId,
            string? txnRef,
            string? bankCode,
            string? bankTranNo,
            string? cardType,
            DateTime paidAt);
    }
}
