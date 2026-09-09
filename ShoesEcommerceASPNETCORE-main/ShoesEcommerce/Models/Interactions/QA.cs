using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Interactions
{
    public class QA
    {
        public int Id { get; set; }

        public int? CustomerId { get; set; }
        public Customer Customer { get; set; }

        public int? StaffId { get; set; }
        public Staff Staff { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int TopicId { get; set; }
        public Topic Topic { get; set; }

        public string Question { get; set; }
        public string Answer { get; set; }

        public DateTime AskedAt { get; set; }
        public DateTime? AnsweredAt { get; set; }
    }
}
