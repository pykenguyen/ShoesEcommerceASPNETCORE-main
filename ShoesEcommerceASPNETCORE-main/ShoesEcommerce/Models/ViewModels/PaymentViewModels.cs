using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShoesEcommerce.Models.ViewModels
{
    /// <summary>
    /// Request model for creating PayPal order from client-side
    /// </summary>
    public class CreatePayPalOrderRequest
    {
        [JsonPropertyName("orderId")]
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid order ID")]
        public int OrderId { get; set; }

        [JsonPropertyName("subtotal")]
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Subtotal must be greater than 0")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("discountAmount")]
        [Range(0, double.MaxValue, ErrorMessage = "Discount amount cannot be negative")]
        public decimal DiscountAmount { get; set; }

        [JsonPropertyName("totalAmount")]
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        public decimal TotalAmount { get; set; }
    }
}
