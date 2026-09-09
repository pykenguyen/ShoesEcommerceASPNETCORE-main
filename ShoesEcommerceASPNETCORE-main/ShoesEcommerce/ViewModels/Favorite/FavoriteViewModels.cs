namespace ShoesEcommerce.ViewModels.Favorite
{
    public class FavoriteItemViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public bool IsInStock { get; set; }
        public DateTime AddedAt { get; set; }
        public string ProductSlug { get; set; } = string.Empty;
    }

    public class FavoriteListViewModel
    {
        public IEnumerable<FavoriteItemViewModel> Favorites { get; set; } = new List<FavoriteItemViewModel>();
        public int TotalCount { get; set; }
    }
}
