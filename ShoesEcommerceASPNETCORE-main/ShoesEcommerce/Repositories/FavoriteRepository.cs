using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Repositories.Interfaces;

namespace ShoesEcommerce.Repositories
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly AppDbContext _context;

        public FavoriteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Favorite>> GetFavoritesByCustomerIdAsync(int customerId)
        {
            return await _context.Favorites
                .Include(f => f.Product)
                    .ThenInclude(p => p.Brand)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Category)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Variants)
                .Where(f => f.CustomerId == customerId)
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();
        }

        public async Task<Favorite?> GetFavoriteAsync(int customerId, int productId)
        {
            return await _context.Favorites
                .FirstOrDefaultAsync(f => f.CustomerId == customerId && f.ProductId == productId);
        }

        public async Task<bool> IsFavoriteAsync(int customerId, int productId)
        {
            return await _context.Favorites
                .AnyAsync(f => f.CustomerId == customerId && f.ProductId == productId);
        }

        public async Task<Favorite> AddFavoriteAsync(Favorite favorite)
        {
            favorite.AddedAt = DateTime.UtcNow;
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            return favorite;
        }

        public async Task<bool> RemoveFavoriteAsync(int customerId, int productId)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.CustomerId == customerId && f.ProductId == productId);

            if (favorite == null)
                return false;

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetFavoriteCountAsync(int productId)
        {
            return await _context.Favorites
                .CountAsync(f => f.ProductId == productId);
        }

        public async Task<IEnumerable<int>> GetFavoriteProductIdsAsync(int customerId)
        {
            return await _context.Favorites
                .Where(f => f.CustomerId == customerId)
                .Select(f => f.ProductId)
                .ToListAsync();
        }
    }
}
