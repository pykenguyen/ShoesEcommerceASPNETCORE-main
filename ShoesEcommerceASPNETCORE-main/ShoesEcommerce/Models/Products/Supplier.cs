using ShoesEcommerce.Models.Stocks;


namespace ShoesEcommerce.Models.Products
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;

        public ICollection<StockEntry> StockEntries { get; set; } = new List<StockEntry>();
    }
}
