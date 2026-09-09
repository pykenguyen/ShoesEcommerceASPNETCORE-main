using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Promotions
{
    public class DiscountProduct
    {
        public int Id { get; set; }
        
        public int DiscountId { get; set; }
        public Discount Discount { get; set; }
        
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
