using ShoesEcommerce.Models.Orders;

namespace ShoesEcommerce.Services.Interfaces
{
    /// <summary>
    /// Email service interface for sending transactional emails
    /// </summary>
    public interface IEmailService
    {
        // ==================== ORDER EMAILS ====================
        
        /// <summary>
        /// Send order confirmation email after successful payment
        /// </summary>
        Task<bool> SendOrderConfirmationAsync(Order order);
        
        /// <summary>
        /// Send email when admin confirms/processes order
        /// </summary>
        Task<bool> SendOrderProcessingAsync(Order order);
        
        /// <summary>
        /// Send email when order is shipped
        /// </summary>
        Task<bool> SendOrderShippedAsync(Order order, string? trackingNumber = null);
        
        /// <summary>
        /// Send email when order is delivered
        /// </summary>
        Task<bool> SendOrderDeliveredAsync(Order order);
        
        /// <summary>
        /// Send email when order is cancelled
        /// </summary>
        Task<bool> SendOrderCancelledAsync(Order order, string? reason = null);
        
        // ==================== CART/REMINDER EMAILS ====================
        
        /// <summary>
        /// Send abandoned cart reminder email
        /// </summary>
        Task<bool> SendAbandonedCartReminderAsync(string email, string customerName, List<CartItemEmailModel> items);
        
        // ==================== ACCOUNT EMAILS ====================
        
        /// <summary>
        /// Send account activation email with verification link
        /// </summary>
        Task<bool> SendAccountActivationAsync(string email, string customerName, string activationLink);
        
        /// <summary>
        /// Send password reset email
        /// </summary>
        Task<bool> SendPasswordResetAsync(string email, string customerName, string resetLink);
        
        /// <summary>
        /// Send welcome email after successful registration
        /// </summary>
        Task<bool> SendWelcomeEmailAsync(string email, string customerName);
        
        // ==================== GENERIC EMAIL ====================
        
        /// <summary>
        /// Send a generic email with custom subject and body
        /// </summary>
        Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
        
        /// <summary>
        /// Send email to multiple recipients
        /// </summary>
        Task<bool> SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string htmlBody);
    }
    
    /// <summary>
    /// Model for cart items in email
    /// </summary>
    public class CartItemEmailModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string VariantInfo { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }
}
