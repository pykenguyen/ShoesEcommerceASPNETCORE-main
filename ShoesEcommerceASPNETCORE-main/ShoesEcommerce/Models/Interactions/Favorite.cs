using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Interactions
{
    public class Favorite
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
