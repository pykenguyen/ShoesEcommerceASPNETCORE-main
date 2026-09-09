using ShoesEcommerce.Models.Interactions;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface IFavoriteRepository
    {
        Task<IEnumerable<Favorite>> GetFavoritesByCustomerIdAsync(int customerId);
        Task<Favorite?> GetFavoriteAsync(int customerId, int productId);
        Task<bool> IsFavoriteAsync(int customerId, int productId);
        Task<Favorite> AddFavoriteAsync(Favorite favorite);
        Task<bool> RemoveFavoriteAsync(int customerId, int productId);
        Task<int> GetFavoriteCountAsync(int productId);
        Task<IEnumerable<int>> GetFavoriteProductIdsAsync(int customerId);
    }
}
