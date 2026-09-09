using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.ViewModels.Favorite;

namespace ShoesEcommerce.Services.Interfaces
{
    public interface IFavoriteService
    {
        Task<IEnumerable<FavoriteItemViewModel>> GetFavoritesByCustomerIdAsync(int customerId);
        Task<bool> IsFavoriteAsync(int customerId, int productId);
        Task<ToggleFavoriteResult> ToggleFavoriteAsync(int customerId, int productId);
        Task<bool> AddToFavoriteAsync(int customerId, int productId);
        Task<bool> RemoveFromFavoriteAsync(int customerId, int productId);
        Task<int> GetFavoriteCountAsync(int productId);
        Task<IEnumerable<int>> GetFavoriteProductIdsAsync(int customerId);
    }

    public class ToggleFavoriteResult
    {
        public bool Success { get; set; }
        public bool IsFavorite { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
