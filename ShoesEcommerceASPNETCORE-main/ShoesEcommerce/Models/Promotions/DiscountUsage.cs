namespace ShoesEcommerce.Models.Promotions
{
    public class DiscountUsage
    {
        public int Id { get; set; }

        public int DiscountId { get; set; }
        public Discount Discount { get; set; }

        public string CustomerEmail { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty; // For guest users

        public int? OrderId { get; set; } // Reference to order if applicable

        public decimal DiscountAmount { get; set; }
        public DateTime UsedAt { get; set; }
    }
}
