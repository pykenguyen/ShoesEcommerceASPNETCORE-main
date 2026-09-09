using ShoesEcommerce.Helpers;
using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Favorite;

namespace ShoesEcommerce.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly ILogger<FavoriteService> _logger;

        public FavoriteService(IFavoriteRepository favoriteRepository, ILogger<FavoriteService> logger)
        {
            _favoriteRepository = favoriteRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<FavoriteItemViewModel>> GetFavoritesByCustomerIdAsync(int customerId)
        {
            try
            {
                var favorites = await _favoriteRepository.GetFavoritesByCustomerIdAsync(customerId);
                
                return favorites.Select(f => new FavoriteItemViewModel
                {
                    Id = f.Id,
                    ProductId = f.ProductId,
                    ProductName = f.Product?.Name ?? "Không có tên",
                    ImageUrl = f.Product?.Variants?.FirstOrDefault()?.ImageUrl,
                    BrandName = f.Product?.Brand?.Name ?? "Không rõ",
                    CategoryName = f.Product?.Category?.Name ?? "Không rõ",
                    MinPrice = f.Product?.Variants?.Any() == true 
                        ? f.Product.Variants.Min(v => v.Price) 
                        : 0,
                    MaxPrice = f.Product?.Variants?.Any() == true 
                        ? f.Product.Variants.Max(v => v.Price) 
                        : 0,
                    IsInStock = f.Product?.Variants?.Any(v => v.IsInStock) ?? false,
                    AddedAt = f.AddedAt,
                    ProductSlug = f.Product?.Name?.ToSlugWithId(f.ProductId) ?? f.ProductId.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorites for customer {CustomerId}", customerId);
                return Enumerable.Empty<FavoriteItemViewModel>();
            }
        }

        public async Task<bool> IsFavoriteAsync(int customerId, int productId)
        {
            return await _favoriteRepository.IsFavoriteAsync(customerId, productId);
        }

        public async Task<ToggleFavoriteResult> ToggleFavoriteAsync(int customerId, int productId)
        {
            try
            {
                var isFavorite = await _favoriteRepository.IsFavoriteAsync(customerId, productId);

                if (isFavorite)
                {
                    await _favoriteRepository.RemoveFavoriteAsync(customerId, productId);
                    _logger.LogInformation("Removed product {ProductId} from favorites for customer {CustomerId}", productId, customerId);
                    return new ToggleFavoriteResult
                    {
                        Success = true,
                        IsFavorite = false,
                        Message = "?ã xóa kh?i danh sách yêu thích"
                    };
                }
                else
                {
                    var favorite = new Favorite
                    {
                        CustomerId = customerId,
                        ProductId = productId,
                        AddedAt = DateTime.UtcNow
                    };
                    await _favoriteRepository.AddFavoriteAsync(favorite);
                    _logger.LogInformation("Added product {ProductId} to favorites for customer {CustomerId}", productId, customerId);
                    return new ToggleFavoriteResult
                    {
                        Success = true,
                        IsFavorite = true,
                        Message = "?ã thêm vào danh sách yêu thích"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for customer {CustomerId} and product {ProductId}", customerId, productId);
                return new ToggleFavoriteResult
                {
                    Success = false,
                    IsFavorite = false,
                    Message = "Có l?i x?y ra. Vui lòng th? l?i."
                };
            }
        }

        public async Task<bool> AddToFavoriteAsync(int customerId, int productId)
        {
            try
            {
                if (await _favoriteRepository.IsFavoriteAsync(customerId, productId))
                    return true;

                var favorite = new Favorite
                {
                    CustomerId = customerId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow
                };
                await _favoriteRepository.AddFavoriteAsync(favorite);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to favorites for customer {CustomerId} and product {ProductId}", customerId, productId);
                return false;
            }
        }

        public async Task<bool> RemoveFromFavoriteAsync(int customerId, int productId)
        {
            try
            {
                return await _favoriteRepository.RemoveFavoriteAsync(customerId, productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from favorites for customer {CustomerId} and product {ProductId}", customerId, productId);
                return false;
            }
        }

        public async Task<int> GetFavoriteCountAsync(int productId)
        {
            return await _favoriteRepository.GetFavoriteCountAsync(productId);
        }

        public async Task<IEnumerable<int>> GetFavoriteProductIdsAsync(int customerId)
        {
            return await _favoriteRepository.GetFavoriteProductIdsAsync(customerId);
        }
    }
}
