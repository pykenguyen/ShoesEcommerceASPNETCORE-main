namespace ShoesEcommerce.Models.Interactions
{
    public class Topic
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<QA> QAs { get; set; }
    }
}
