using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Promotions;
using ShoesEcommerce.Repositories.Interfaces;

namespace ShoesEcommerce.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        // Product CRUD
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ProductExistsAsync(int id)
        {
            return await _context.Products.AnyAsync(p => p.Id == id);
        }

        // Product Variants
        public async Task<IEnumerable<ProductVariant>> GetProductVariantsAsync(int productId)
        {
            return await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.CurrentStock) // ? INCLUDE STOCK DATA
                .Where(v => v.ProductId == productId)
                .ToListAsync();
        }

        public async Task<ProductVariant?> GetProductVariantByIdAsync(int id)
        {
            return await _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Category)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Brand)
                .Include(v => v.CurrentStock) // ? INCLUDE STOCK DATA
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<ProductVariant> CreateProductVariantAsync(ProductVariant variant)
        {
            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();
            return variant;
        }

        public async Task<ProductVariant> UpdateProductVariantAsync(ProductVariant variant)
        {
            _context.Entry(variant).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return variant;
        }

        public async Task<bool> DeleteProductVariantAsync(int id)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null)
                return false;

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
            return true;
        }

        // Categories
        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        // Brands
        public async Task<IEnumerable<Brand>> GetAllBrandsAsync()
        {
            return await _context.Brands
                .Include(b => b.Products)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<Brand?> GetBrandByIdAsync(int id)
        {
            return await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Brand> CreateBrandAsync(Brand brand)
        {
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
            return brand;
        }

        public async Task<Brand> UpdateBrandAsync(Brand brand)
        {
            _context.Entry(brand).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return brand;
        }

        public async Task<bool> DeleteBrandAsync(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
                return false;

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            return true;
        }

        // Suppliers
        public async Task<IEnumerable<Supplier>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            return await _context.Suppliers
                .Include(s => s.StockEntries)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return supplier;
        }

        public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
        {
            _context.Entry(supplier).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return supplier;
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return false;

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            return true;
        }

        // Advanced Queries
        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllProductsAsync();

            searchTerm = searchTerm.ToLower();

            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm) ||
                    p.Category.Name.ToLower().Contains(searchTerm) ||
                    p.Brand.Name.ToLower().Contains(searchTerm))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Where(p => p.CategoryId == categoryId)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Where(p => p.BrandId == brandId)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetPaginatedProductsAsync(int pageNumber, int pageSize, string searchTerm = "", int? categoryId = null, int? brandId = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm) ||
                    p.Category.Name.ToLower().Contains(searchTerm) ||
                    p.Brand.Name.ToLower().Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            return await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalProductCountAsync(string searchTerm = "", int? categoryId = null, int? brandId = null)
        {
            var query = _context.Products.AsQueryable();

            // Apply same filters as GetPaginatedProductsAsync
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm) ||
                    p.Category.Name.ToLower().Contains(searchTerm) ||
                    p.Brand.Name.ToLower().Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            return await query.CountAsync();
        }

        // ? ADD: Discount-specific repository methods
        public async Task<IEnumerable<Product>> GetProductsWithDiscountsAsync(int page, int pageSize)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.DiscountProducts)
                    .ThenInclude(dp => dp.Discount)
                .Where(p => p.DiscountProducts.Any(dp => 
                    dp.Discount.IsActive && 
                    dp.Discount.StartDate <= DateTime.Now && 
                    dp.Discount.EndDate >= DateTime.Now))
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithActiveDiscountAsync(int productId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.DiscountProducts)
                    .ThenInclude(dp => dp.Discount)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<IEnumerable<Product>> GetFeaturedDiscountProductsAsync(int count = 10)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.DiscountProducts)
                    .ThenInclude(dp => dp.Discount)
                .Where(p => p.DiscountProducts.Any(dp => 
                    dp.Discount.IsActive && 
                    dp.Discount.IsFeatured &&
                    dp.Discount.StartDate <= DateTime.Now && 
                    dp.Discount.EndDate >= DateTime.Now))
                .OrderByDescending(p => p.DiscountProducts
                    .Where(dp => dp.Discount.IsActive && dp.Discount.IsFeatured)
                    .Max(dp => dp.Discount.CreatedAt))
                .Take(count)
                .ToListAsync();
        }

        public async Task<Discount?> GetActiveDiscountForProductAsync(int productId)
        {
            return await _context.DiscountProducts
                .Where(dp => dp.ProductId == productId && 
                            dp.Discount.IsActive && 
                            dp.Discount.StartDate <= DateTime.Now && 
                            dp.Discount.EndDate >= DateTime.Now)
                .Select(dp => dp.Discount)
                .OrderByDescending(d => d.IsFeatured)
                .ThenByDescending(d => d.Type == DiscountType.Percentage ? d.PercentageValue : d.FixedValue)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Discount>> GetFeaturedDiscountsAsync()
        {
            return await _context.Discounts
                .Where(d => d.IsActive && d.IsFeatured && 
                           d.StartDate <= DateTime.Now && 
                           d.EndDate >= DateTime.Now)
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .ToListAsync();
        }

        // ? NEW: Product Variant List Queries - for displaying variants directly
        public async Task<IEnumerable<ProductVariant>> GetPaginatedProductVariantsAsync(int pageNumber, int pageSize, string searchTerm = "", int? categoryId = null, int? brandId = null)
        {
            var query = _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Category)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Brand)
                .Include(v => v.CurrentStock)
                .Include(v => v.Product)
                    .ThenInclude(p => p.DiscountProducts)
                        .ThenInclude(dp => dp.Discount)
                .AsQueryable();

            // Apply filters based on product properties
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(v =>
                    v.Product.Name.ToLower().Contains(searchTerm) ||
                    v.Product.Description.ToLower().Contains(searchTerm) ||
                    v.Product.Category.Name.ToLower().Contains(searchTerm) ||
                    v.Product.Brand.Name.ToLower().Contains(searchTerm) ||
                    v.Color.ToLower().Contains(searchTerm) ||
                    v.Size.ToLower().Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(v => v.Product.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(v => v.Product.BrandId == brandId.Value);
            }

            return await query
                .OrderBy(v => v.Product.Name)
                .ThenBy(v => v.Color)
                .ThenBy(v => v.Size)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalProductVariantCountAsync(string searchTerm = "", int? categoryId = null, int? brandId = null)
        {
            var query = _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Category)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Brand)
                .AsQueryable();

            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(v =>
                    v.Product.Name.ToLower().Contains(searchTerm) ||
                    v.Product.Description.ToLower().Contains(searchTerm) ||
                    v.Product.Category.Name.ToLower().Contains(searchTerm) ||
                    v.Product.Brand.Name.ToLower().Contains(searchTerm) ||
                    v.Color.ToLower().Contains(searchTerm) ||
                    v.Size.ToLower().Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(v => v.Product.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(v => v.Product.BrandId == brandId.Value);
            }

            return await query.CountAsync();
        }

        public async Task<IEnumerable<ProductVariant>> GetFeaturedProductVariantsAsync(int count = 8)
        {
            return await _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Category)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Brand)
                .Include(v => v.CurrentStock)
                .Include(v => v.Product)
                    .ThenInclude(p => p.DiscountProducts)
                        .ThenInclude(dp => dp.Discount)
                .Where(v => v.CurrentStock != null && v.CurrentStock.AvailableQuantity > 0) 
                .OrderByDescending(v => v.Id) 
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductVariant>> GetDiscountedProductVariantsAsync(int page = 1, int pageSize = 12)
        {
            return await _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Category)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Brand)
                .Include(v => v.CurrentStock)
                .Include(v => v.Product)
                    .ThenInclude(p => p.DiscountProducts)
                        .ThenInclude(dp => dp.Discount)
                .Where(v => v.Product.DiscountProducts.Any(dp => 
                    dp.Discount.IsActive && 
                    dp.Discount.StartDate <= DateTime.Now && 
                    dp.Discount.EndDate >= DateTime.Now))
                .OrderByDescending(v => v.Product.DiscountProducts
                    .Where(dp => dp.Discount.IsActive)
                    .Max(dp => dp.Discount.Type == DiscountType.Percentage ? dp.Discount.PercentageValue : dp.Discount.FixedValue))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductVariant>> GetAllProductVariantsAsync()
        {
            return await _context.ProductVariants
                .Include(v => v.Product)
                .ToListAsync();
        }
    }
}