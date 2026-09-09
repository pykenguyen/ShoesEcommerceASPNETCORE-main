using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Promotions;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface IProductRepository
    {
        // Product CRUD
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> ProductExistsAsync(int id);

        // Product Variants
        Task<IEnumerable<ProductVariant>> GetProductVariantsAsync(int productId);
        Task<ProductVariant?> GetProductVariantByIdAsync(int id);
        Task<ProductVariant> CreateProductVariantAsync(ProductVariant variant);
        Task<ProductVariant> UpdateProductVariantAsync(ProductVariant variant);
        Task<bool> DeleteProductVariantAsync(int id);

        // Categories
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int id);

        // Brands
        Task<IEnumerable<Brand>> GetAllBrandsAsync();
        Task<Brand?> GetBrandByIdAsync(int id);
        Task<Brand> CreateBrandAsync(Brand brand);
        Task<Brand> UpdateBrandAsync(Brand brand);
        Task<bool> DeleteBrandAsync(int id);

        // Suppliers
        Task<IEnumerable<Supplier>> GetAllSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int id);
        Task<Supplier> CreateSupplierAsync(Supplier supplier);
        Task<Supplier> UpdateSupplierAsync(Supplier supplier);
        Task<bool> DeleteSupplierAsync(int id);

        // Advanced Queries
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByBrandAsync(int brandId);
        Task<IEnumerable<Product>> GetPaginatedProductsAsync(int pageNumber, int pageSize, string searchTerm = "", int? categoryId = null, int? brandId = null);
        Task<int> GetTotalProductCountAsync(string searchTerm = "", int? categoryId = null, int? brandId = null);

        // ✅ NEW: Product Variant List Queries - for displaying variants directly
        Task<IEnumerable<ProductVariant>> GetPaginatedProductVariantsAsync(int pageNumber, int pageSize, string searchTerm = "", int? categoryId = null, int? brandId = null);
        Task<int> GetTotalProductVariantCountAsync(string searchTerm = "", int? categoryId = null, int? brandId = null);
        Task<IEnumerable<ProductVariant>> GetFeaturedProductVariantsAsync(int count = 8);
        Task<IEnumerable<ProductVariant>> GetDiscountedProductVariantsAsync(int page = 1, int pageSize = 12);

        // ✅ ADD: Discount-specific repository methods
        Task<IEnumerable<Product>> GetProductsWithDiscountsAsync(int page, int pageSize);
        Task<Product?> GetProductWithActiveDiscountAsync(int productId);
        Task<IEnumerable<Product>> GetFeaturedDiscountProductsAsync(int count = 10);
        Task<Discount?> GetActiveDiscountForProductAsync(int productId);
        Task<IEnumerable<Discount>> GetFeaturedDiscountsAsync();

        // Admin Report Data
        Task<IEnumerable<ProductVariant>> GetAllProductVariantsAsync();
    }
}