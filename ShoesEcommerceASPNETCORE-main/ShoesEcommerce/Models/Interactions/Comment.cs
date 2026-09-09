using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Interactions
{
    public class Comment
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }
    }   
}
