using ShoesEcommerce.Models.Payments.PayPal;
using ShoesEcommerce.Models.Orders;

namespace ShoesEcommerce.Services.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// Create PayPal order with discount support and item details
        /// </summary>
        Task<CreateOrderResponse> CreatePayPalOrderAsync(
            int orderId, 
            decimal subtotal, 
            decimal discountAmount, 
            decimal totalAmount, 
            string returnUrl, 
            string cancelUrl);

        /// <summary>
        /// Capture PayPal payment and update order status
        /// </summary>
        Task<CaptureOrderResponse> CapturePayPalOrderAsync(string paypalOrderId);

        /// <summary>
        /// Update payment status in database
        /// </summary>
        Task<bool> UpdatePaymentStatusAsync(int orderId, string status, DateTime? paidAt = null, string? transactionId = null);

        /// <summary>
        /// Get payment details for an order
        /// </summary>
        Task<Models.Orders.Payment?> GetPaymentByOrderIdAsync(int orderId);

        /// <summary>
        /// Create payment record for an order
        /// </summary>
        Task<Models.Orders.Payment> CreatePaymentAsync(int orderId, string method, string status);

        /// <summary>
        /// Verify PayPal payment status
        /// </summary>
        Task<bool> VerifyPayPalPaymentAsync(string paypalOrderId);

        /// <summary>
        /// Complete payment and update invoice
        /// </summary>
        Task<bool> CompletePaymentAsync(int orderId, string transactionId, DateTime paidAt);

        /// <summary>
        /// Handle payment failure with proper rollback (cancels invoice, updates order status)
        /// </summary>
        Task<bool> HandlePaymentFailureAsync(int orderId, string reason);

        /// <summary>
        /// Handle user cancelled payment (cancels invoice, updates order status)
        /// </summary>
        Task<bool> HandlePaymentCancellationAsync(int orderId);

        /// <summary>
        /// Prepare VNPay payment: ensure invoice/payment records exist and set to pending
        /// </summary>
        Task<Order> PrepareVnPayPaymentAsync(int orderId);

        /// <summary>
        /// Complete VNPay payment - update payment status and finalize invoice with VNPay data
        /// </summary>
        Task<bool> CompleteVnPayPaymentAsync(int orderId, string transactionId, string? bankCode, DateTime paidAt);

        /// <summary>
        /// Complete VNPay payment with full data - update payment status and finalize invoice with all VNPay response fields
        /// </summary>
        Task<bool> CompleteVnPayPaymentFullAsync(
            int orderId, 
            string transactionId, 
            string? txnRef,
            string? bankCode, 
            string? bankTranNo,
            string? cardType,
            DateTime paidAt);
    }
}
