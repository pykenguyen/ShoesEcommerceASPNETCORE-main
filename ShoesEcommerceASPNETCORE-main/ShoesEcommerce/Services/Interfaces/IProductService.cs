using ShoesEcommerce.Models.Products;
using ShoesEcommerce.ViewModels.Product;

namespace ShoesEcommerce.Services.Interfaces
{
    public interface IProductService
    {
        // Product Management
        Task<ProductListViewModel> GetProductsAsync(string searchTerm, int? categoryId, int? brandId, int page, int pageSize);
        Task<Product?> GetProductByIdAsync(int id);
        Task<ProductInfo> CreateProductAsync(CreateProductViewModel model);
        Task<bool> UpdateProductAsync(int id, EditProductViewModel model);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> ProductExistsAsync(int id);

        // ? NEW: Product Variant List Methods - Display variants instead of products
        Task<ProductVariantListViewModel> GetProductVariantsListAsync(string searchTerm, int? categoryId, int? brandId, int page, int pageSize);
        Task<IEnumerable<ProductVariantDisplayInfo>> GetFeaturedProductVariantsAsync(int count = 8);
        Task<IEnumerable<ProductVariantDisplayInfo>> GetDiscountedProductVariantsAsync(int page = 1, int pageSize = 12);

        // Product Variants
        Task<IEnumerable<ProductVariantInfo>> GetProductVariantsAsync(int productId);
        Task<ProductVariant?> GetProductVariantByIdAsync(int id);
        Task<ProductVariantInfo> CreateProductVariantAsync(CreateProductVariantViewModel model);
        Task<bool> UpdateProductVariantAsync(int id, EditProductVariantViewModel model);
        Task<bool> DeleteProductVariantAsync(int id);

        // Categories
        Task<IEnumerable<CategoryInfo>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<CategoryInfo> CreateCategoryAsync(CreateCategoryViewModel model);
        Task<bool> UpdateCategoryAsync(int id, EditCategoryViewModel model);
        Task<bool> DeleteCategoryAsync(int id);

        // Brands
        Task<IEnumerable<BrandInfo>> GetAllBrandsAsync();
        Task<Brand?> GetBrandByIdAsync(int id);
        Task<BrandInfo> CreateBrandAsync(CreateBrandViewModel model);
        Task<bool> UpdateBrandAsync(int id, EditBrandViewModel model);
        Task<bool> DeleteBrandAsync(int id);

        // Suppliers
        Task<IEnumerable<SupplierInfo>> GetAllSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int id);
        Task<SupplierInfo> CreateSupplierAsync(CreateSupplierViewModel model);
        Task<bool> UpdateSupplierAsync(int id, EditSupplierViewModel model);
        Task<bool> DeleteSupplierAsync(int id);

        // Dropdown Data
        Task<IEnumerable<Category>> GetCategoriesForDropdownAsync();
        Task<IEnumerable<Brand>> GetBrandsForDropdownAsync();
        Task<IEnumerable<Supplier>> GetSuppliersForDropdownAsync();
        Task<IEnumerable<ProductVariant>> GetProductVariantsForDropdownAsync();

        // Validation
        Task<bool> ValidateProductDataAsync(CreateProductViewModel model);
        Task<bool> ValidateProductUpdateAsync(int id, EditProductViewModel model);
        Task<bool> CanDeleteProductAsync(int id);
    }
}