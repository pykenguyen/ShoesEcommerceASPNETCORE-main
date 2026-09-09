using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Product;

namespace ShoesEcommerce.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IFileUploadService _fileUploadService;
        private readonly IStockService _stockService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository productRepository, IFileUploadService fileUploadService, IStockService stockService, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _fileUploadService = fileUploadService;
            _stockService = stockService;
            _logger = logger;
        }

        // Product Management
        public async Task<ProductListViewModel> GetProductsAsync(string searchTerm, int? categoryId, int? brandId, int page, int pageSize)
        {
            try
            {
                var products = await _productRepository.GetPaginatedProductsAsync(page, pageSize, searchTerm, categoryId, brandId);
                var totalCount = await _productRepository.GetTotalProductCountAsync(searchTerm, categoryId, brandId);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Map to ProductInfo
                var productInfos = products.Select(p => new ProductInfo
                {
                    Id = p.Id,
                    Name = p.Name ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    MinPrice = p.Variants?.Any() == true ? p.Variants.Min(v => v.Price) : 0,
                    MaxPrice = p.Variants?.Any() == true ? p.Variants.Max(v => v.Price) : 0,
                    Price = p.Variants?.Any() == true ? p.Variants.Min(v => v.Price) : 0, // Set Price = MinPrice for backward compatibility
                    CategoryName = p.Category?.Name ?? "N/A",
                    BrandName = p.Brand?.Name ?? "N/A",
                    VariantCount = p.Variants?.Count ?? 0,
                    // ? FIX: Calculate stock information from variants
                    TotalStock = p.Variants?.Sum(v => v.AvailableQuantity) ?? 0,
                    IsInStock = p.Variants?.Any(v => v.AvailableQuantity > 0) ?? false,
                    CreatedDate = DateTime.Now, // Add this field to your model if needed
                    IsActive = true, // Add this field to your model if needed
                    // ? FIX: Set ImageUrl to first variant's image or use SVG placeholder
                    ImageUrl = p.Variants?.FirstOrDefault(v => !string.IsNullOrEmpty(v.ImageUrl))?.ImageUrl ?? "/images/no-image.svg"
                }).ToList();

                return new ProductListViewModel
                {
                    Products = productInfos,
                    SearchTerm = searchTerm ?? string.Empty,
                    CategoryId = categoryId,
                    BrandId = brandId,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalItems = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting products");
                return new ProductListViewModel
                {
                    Products = new List<ProductInfo>(),
                    SearchTerm = searchTerm ?? string.Empty,
                    CategoryId = categoryId,
                    BrandId = brandId,
                    CurrentPage = page,
                    TotalPages = 0,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalItems = 0
                };
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetProductByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product by ID: {ProductId}", id);
                return null;
            }
        }

        public async Task<ProductInfo> CreateProductAsync(CreateProductViewModel model)
        {
            try
            {
                if (!await ValidateProductDataAsync(model))
                    throw new InvalidOperationException("Product data validation failed");

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    BrandId = model.BrandId
                };

                var createdProduct = await _productRepository.CreateProductAsync(product);

                // Get related data for return value
                var category = await _productRepository.GetCategoryByIdAsync(model.CategoryId);
                var brand = await _productRepository.GetBrandByIdAsync(model.BrandId);

                return new ProductInfo
                {
                    Id = createdProduct.Id,
                    Name = createdProduct.Name,
                    Description = createdProduct.Description,
                    MinPrice = 0, // No price until variants are added
                    MaxPrice = 0,
                    Price = 0, // No price until variants are added
                    CategoryName = category?.Name ?? "N/A",
                    BrandName = brand?.Name ?? "N/A",
                    VariantCount = 0,
                    TotalStock = 0,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating product");
                throw new InvalidOperationException("Unable to create product", ex);
            }
        }

        public async Task<bool> UpdateProductAsync(int id, EditProductViewModel model)
        {
            try
            {
                if (!await ValidateProductUpdateAsync(id, model))
                    return false;

                var existingProduct = await _productRepository.GetProductByIdAsync(id);
                if (existingProduct == null)
                    return false;

                existingProduct.Name = model.Name;
                existingProduct.Description = model.Description;
                existingProduct.CategoryId = model.CategoryId;
                existingProduct.BrandId = model.BrandId;

                await _productRepository.UpdateProductAsync(existingProduct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating product with ID: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                if (!await CanDeleteProductAsync(id))
                    return false;

                return await _productRepository.DeleteProductAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting product with ID: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> ProductExistsAsync(int id)
        {
            try
            {
                return await _productRepository.ProductExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if product exists: {ProductId}", id);
                return false;
            }
        }

        // Product Variants
        public async Task<IEnumerable<ProductVariantInfo>> GetProductVariantsAsync(int productId)
        {
            try
            {
                var variants = await _productRepository.GetProductVariantsAsync(productId);
                return variants.Select(v => new ProductVariantInfo
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    ProductName = v.Product?.Name ?? "N/A",
                    Color = v.Color ?? string.Empty,
                    Size = v.Size ?? string.Empty,
                    ImageUrl = v.ImageUrl ?? string.Empty,
                    Price = v.Price,
                    StockQuantity = v.AvailableQuantity // ? USE COMPUTED PROPERTY
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants for product: {ProductId}", productId);
                return new List<ProductVariantInfo>();
            }
        }

        public async Task<ProductVariant?> GetProductVariantByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetProductVariantByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variant by ID: {VariantId}", id);
                return null;
            }
        }

        public async Task<ProductVariantInfo> CreateProductVariantAsync(CreateProductVariantViewModel model)
        {
            try
            {
                _logger.LogInformation("Creating product variant for Product {ProductId}: {Color} - {Size}", 
                    model.ProductId, model.Color, model.Size);

                // Handle image upload - simplified logic
                string imageUrl = string.Empty;
                
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    _logger.LogInformation("Processing image file: {FileName}, Size: {FileSize} bytes", 
                        model.ImageFile.FileName, model.ImageFile.Length);
                    
                    try
                    {
                        // Upload file and get URL
                        imageUrl = await _fileUploadService.UploadProductVariantImageAsync(
                            model.ImageFile, 
                            model.ProductId, 
                            model.Color, 
                            model.Size);
                        
                        _logger.LogInformation("Image uploaded successfully: {ImageUrl}", imageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload image file");
                        // Continue without image - don't fail the variant creation
                        imageUrl = string.Empty;
                    }
                }
                else
                {
                    _logger.LogInformation("No image file provided for product variant");
                }

                var variant = new ProductVariant
                {
                    ProductId = model.ProductId,
                    Color = model.Color,
                    Size = model.Size,
                    Price = model.Price,
                    ImageUrl = imageUrl ?? string.Empty // Ensure never null
                };

                _logger.LogInformation("Creating product variant entity with ImageUrl: '{ImageUrl}'", variant.ImageUrl);
                var createdVariant = await _productRepository.CreateProductVariantAsync(variant);
                
                _logger.LogInformation("Product variant created successfully with ID: {VariantId}", createdVariant.Id);
                
                // Create initial stock if quantity is provided
                if (model.InitialStockQuantity > 0)
                {
                    _logger.LogInformation("Creating initial stock of {Quantity} for variant {VariantId}", 
                        model.InitialStockQuantity, createdVariant.Id);
                    
                    try
                    {
                        // Use AdjustStockAsync instead which doesn't require a supplier
                        var stockCreated = await _stockService.AdjustStockAsync(
                            createdVariant.Id, 
                            model.InitialStockQuantity, 
                            "Initial stock when creating product variant",
                            "System"
                        );
                        
                        if (stockCreated)
                        {
                            _logger.LogInformation("Initial stock created successfully for variant {VariantId}", createdVariant.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create initial stock for variant {VariantId}", createdVariant.Id);
                            // Don't fail the variant creation if stock creation fails, just log it
                        }
                    }
                    catch (Exception stockEx)
                    {
                        _logger.LogError(stockEx, "Exception occurred while creating initial stock for variant {VariantId}", createdVariant.Id);
                        // Don't throw here - variant creation succeeded, stock creation is secondary
                    }
                }
                else
                {
                    _logger.LogInformation("No initial stock quantity specified for variant {VariantId}", createdVariant.Id);
                }
                
                // Get product info for return value
                var product = await _productRepository.GetProductByIdAsync(model.ProductId);

                return new ProductVariantInfo
                {
                    Id = createdVariant.Id,
                    ProductId = createdVariant.ProductId,
                    ProductName = product?.Name ?? "N/A",
                    Color = createdVariant.Color,
                    Size = createdVariant.Size,
                    ImageUrl = createdVariant.ImageUrl,
                    Price = createdVariant.Price,
                    StockQuantity = model.InitialStockQuantity // Use initial value for display
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating product variant for Product {ProductId}", model.ProductId);
                throw;
            }
        }

        public async Task<bool> UpdateProductVariantAsync(int id, EditProductVariantViewModel model)
        {
            try
            {
                _logger.LogInformation("=== UpdateProductVariantAsync START ===");
                _logger.LogInformation("Variant ID: {Id}, ProductId: {ProductId}", id, model.ProductId);
                _logger.LogInformation("KeepCurrentImage: {Keep}, UseImageUrl: {UseUrl}", model.KeepCurrentImage, model.UseImageUrl);
                _logger.LogInformation("ImageFile: {HasFile}, ImageUrl: {ImageUrl}", 
                    model.ImageFile != null ? $"Yes ({model.ImageFile.FileName}, {model.ImageFile.Length} bytes)" : "No", 
                    model.ImageUrl ?? "(null)");

                var existingVariant = await _productRepository.GetProductVariantByIdAsync(id);
                if (existingVariant == null)
                {
                    _logger.LogWarning("Variant not found with ID: {Id}", id);
                    return false;
                }

                _logger.LogInformation("Existing variant ImageUrl: {ImageUrl}", existingVariant.ImageUrl ?? "(null)");

                // Handle image update
                string imageUrl = existingVariant.ImageUrl ?? string.Empty;
                
                // Case 1: New file uploaded
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    _logger.LogInformation("?? Uploading new image file: {FileName}", model.ImageFile.FileName);
                    
                    // Delete old image if it exists (both local and cloud)
                    if (!string.IsNullOrEmpty(existingVariant.ImageUrl))
                    {
                        _logger.LogInformation("??? Deleting old image: {OldUrl}", existingVariant.ImageUrl);
                        await _fileUploadService.DeleteImageAsync(existingVariant.ImageUrl);
                    }
                    
                    // Upload new file
                    imageUrl = await _fileUploadService.UploadProductVariantImageAsync(
                        model.ImageFile, 
                        model.ProductId, 
                        model.Color, 
                        model.Size);
                    
                    _logger.LogInformation("? New image uploaded: {NewUrl}", imageUrl);
                }
                // Case 2: User wants to remove the image (KeepCurrentImage = false and no new file)
                else if (!model.KeepCurrentImage && model.ImageFile == null)
                {
                    _logger.LogInformation("??? Removing current image");
                    
                    if (!string.IsNullOrEmpty(existingVariant.ImageUrl))
                    {
                        await _fileUploadService.DeleteImageAsync(existingVariant.ImageUrl);
                    }
                    imageUrl = string.Empty;
                }
                // Case 3: Keep current image (default)
                else
                {
                    _logger.LogInformation("?? Keeping current image: {CurrentUrl}", imageUrl);
                }

                existingVariant.Color = model.Color;
                existingVariant.Size = model.Size;
                existingVariant.Price = model.Price;
                existingVariant.ImageUrl = imageUrl;

                _logger.LogInformation("?? Saving variant with ImageUrl: {FinalUrl}", existingVariant.ImageUrl);
                await _productRepository.UpdateProductVariantAsync(existingVariant);
                
                _logger.LogInformation("=== UpdateProductVariantAsync SUCCESS ===");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating product variant with ID: {VariantId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteProductVariantAsync(int id)
        {
            try
            {
                // Get variant to delete associated image
                var variant = await _productRepository.GetProductVariantByIdAsync(id);
                
                var result = await _productRepository.DeleteProductVariantAsync(id);
                
                // Delete associated image if it's a local file
                if (result && variant != null && !string.IsNullOrEmpty(variant.ImageUrl) && variant.ImageUrl.StartsWith("/"))
                {
                    await _fileUploadService.DeleteImageAsync(variant.ImageUrl);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting product variant with ID: {VariantId}", id);
                return false;
            }
        }

        // Categories
        public async Task<IEnumerable<CategoryInfo>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _productRepository.GetAllCategoriesAsync();
                return categories.Select(c => new CategoryInfo
                {
                    Id = c.Id,
                    Name = c.Name ?? string.Empty,
                    ProductCount = c.Products?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all categories");
                return new List<CategoryInfo>();
            }
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetCategoryByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting category by ID: {CategoryId}", id);
                return null;
            }
        }

        public async Task<CategoryInfo> CreateCategoryAsync(CreateCategoryViewModel model)
        {
            try
            {
                var category = new Category
                {
                    Name = model.Name,
                    Description = model.Description ?? string.Empty
                };

                var createdCategory = await _productRepository.CreateCategoryAsync(category);

                return new CategoryInfo
                {
                    Id = createdCategory.Id,
                    Name = createdCategory.Name,
                    ProductCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category");
                throw new InvalidOperationException("Unable to create category", ex);
            }
        }

        public async Task<bool> UpdateCategoryAsync(int id, EditCategoryViewModel model)
        {
            try
            {
                var existingCategory = await _productRepository.GetCategoryByIdAsync(id);
                if (existingCategory == null)
                    return false;

                existingCategory.Name = model.Name;
                existingCategory.Description = model.Description ?? string.Empty;

                await _productRepository.UpdateCategoryAsync(existingCategory);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category with ID: {CategoryId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                return await _productRepository.DeleteCategoryAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category with ID: {CategoryId}", id);
                return false;
            }
        }

        // Brands
        public async Task<IEnumerable<BrandInfo>> GetAllBrandsAsync()
        {
            try
            {
                var brands = await _productRepository.GetAllBrandsAsync();
                return brands.Select(b => new BrandInfo
                {
                    Id = b.Id,
                    Name = b.Name ?? string.Empty,
                    ProductCount = b.Products?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all brands");
                return new List<BrandInfo>();
            }
        }

        public async Task<Brand?> GetBrandByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetBrandByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting brand by ID: {BrandId}", id);
                return null;
            }
        }

        public async Task<BrandInfo> CreateBrandAsync(CreateBrandViewModel model)
        {
            try
            {
                var brand = new Brand
                {
                    Name = model.Name
                };

                var createdBrand = await _productRepository.CreateBrandAsync(brand);

                return new BrandInfo
                {
                    Id = createdBrand.Id,
                    Name = createdBrand.Name,
                    ProductCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating brand");
                throw new InvalidOperationException("Unable to create brand", ex);
            }
        }

        public async Task<bool> UpdateBrandAsync(int id, EditBrandViewModel model)
        {
            try
            {
                var existingBrand = await _productRepository.GetBrandByIdAsync(id);
                if (existingBrand == null)
                    return false;

                existingBrand.Name = model.Name;

                await _productRepository.UpdateBrandAsync(existingBrand);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating brand with ID: {BrandId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteBrandAsync(int id)
        {
            try
            {
                return await _productRepository.DeleteBrandAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting brand with ID: {BrandId}", id);
                return false;
            }
        }

        // Suppliers
        public async Task<IEnumerable<SupplierInfo>> GetAllSuppliersAsync()
        {
            try
            {
                var suppliers = await _productRepository.GetAllSuppliersAsync();
                return suppliers.Select(s => new SupplierInfo
                {
                    Id = s.Id,
                    Name = s.Name ?? string.Empty,
                    ContactInfo = s.ContactInfo ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all suppliers");
                return new List<SupplierInfo>();
            }
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            try
            {
                return await _productRepository.GetSupplierByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting supplier by ID: {SupplierId}", id);
                return null;
            }
        }

        public async Task<SupplierInfo> CreateSupplierAsync(CreateSupplierViewModel model)
        {
            try
            {
                var supplier = new Supplier
                {
                    Name = model.Name,
                    ContactInfo = model.ContactInfo
                };

                var createdSupplier = await _productRepository.CreateSupplierAsync(supplier);

                return new SupplierInfo
                {
                    Id = createdSupplier.Id,
                    Name = createdSupplier.Name,
                    ContactInfo = createdSupplier.ContactInfo,
                    StockEntryCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating supplier");
                throw new InvalidOperationException("Unable to create supplier", ex);
            }
        }

        public async Task<bool> UpdateSupplierAsync(int id, EditSupplierViewModel model)
        {
            try
            {
                var existingSupplier = await _productRepository.GetSupplierByIdAsync(id);
                if (existingSupplier == null)
                    return false;

                existingSupplier.Name = model.Name;
                existingSupplier.ContactInfo = model.ContactInfo;

                await _productRepository.UpdateSupplierAsync(existingSupplier);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating supplier with ID: {SupplierId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            try
            {
                return await _productRepository.DeleteSupplierAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting supplier with ID: {SupplierId}", id);
                return false;
            }
        }

        // Dropdown Data
        public async Task<IEnumerable<Category>> GetCategoriesForDropdownAsync()
        {
            try
            {
                return await _productRepository.GetAllCategoriesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting categories for dropdown");
                return new List<Category>();
            }
        }

        public async Task<IEnumerable<Brand>> GetBrandsForDropdownAsync()
        {
            try
            {
                return await _productRepository.GetAllBrandsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting brands for dropdown");
                return new List<Brand>();
            }
        }

        public async Task<IEnumerable<Supplier>> GetSuppliersForDropdownAsync()
        {
            try
            {
                return await _productRepository.GetAllSuppliersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting suppliers for dropdown");
                return new List<Supplier>();
            }
        }

        public async Task<IEnumerable<ProductVariant>> GetProductVariantsForDropdownAsync()
        {
            try
            {
                var products = await _productRepository.GetAllProductsAsync();
                return products.SelectMany(p => p.Variants ?? new List<ProductVariant>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants for dropdown");
                return new List<ProductVariant>();
            }
        }

        // Validation
        public async Task<bool> ValidateProductDataAsync(CreateProductViewModel model)
        {
            try
            {
                // Check if category exists
                var category = await _productRepository.GetCategoryByIdAsync(model.CategoryId);
                if (category == null)
                    return false;

                // Check if brand exists
                var brand = await _productRepository.GetBrandByIdAsync(model.BrandId);
                if (brand == null)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating product data");
                return false;
            }
        }

        public async Task<bool> ValidateProductUpdateAsync(int id, EditProductViewModel model)
        {
            try
            {
                // Check if product exists
                if (!await _productRepository.ProductExistsAsync(id))
                    return false;

                // Check if category exists
                var category = await _productRepository.GetCategoryByIdAsync(model.CategoryId);
                if (category == null)
                    return false;

                // Check if brand exists
                var brand = await _productRepository.GetBrandByIdAsync(model.BrandId);
                if (brand == null)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating product update data for ID: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> CanDeleteProductAsync(int id)
        {
            try
            {
                // Check if product exists
                if (!await _productRepository.ProductExistsAsync(id))
                    return false;

                // Add business rules for deletion (e.g., check if product has orders, etc.)
                // For now, allow deletion
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if product can be deleted: {ProductId}", id);
                return false;
            }
        }

        // ? NEW: Product Variant List Methods - Display variants instead of products
        public async Task<ProductVariantListViewModel> GetProductVariantsListAsync(string searchTerm, int? categoryId, int? brandId, int page, int pageSize)
        {
            try
            {
                var variants = await _productRepository.GetPaginatedProductVariantsAsync(page, pageSize, searchTerm, categoryId, brandId);
                var totalCount = await _productRepository.GetTotalProductVariantCountAsync(searchTerm, categoryId, brandId);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var variantInfos = variants.Select(v => new ProductVariantDisplayInfo
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    ProductName = v.Product?.Name ?? "N/A",
                    ProductDescription = v.Product?.Description ?? string.Empty,
                    CategoryName = v.Product?.Category?.Name ?? "N/A",
                    BrandName = v.Product?.Brand?.Name ?? "N/A",
                    Color = v.Color ?? string.Empty,
                    Size = v.Size ?? string.Empty,
                    ImageUrl = !string.IsNullOrEmpty(v.ImageUrl) ? v.ImageUrl : "/images/no-image.svg",
                    Price = v.Price,
                    StockQuantity = v.AvailableQuantity,
                    HasActiveDiscount = v.HasActiveDiscount,
                    DiscountName = v.GetActiveDiscount()?.Name,
                    DiscountPercentage = v.DiscountPercentage,
                    DiscountAmount = v.DiscountAmount,
                    DiscountedPrice = v.DiscountedPrice,
                    CreatedDate = DateTime.Now
                }).ToList();

                return new ProductVariantListViewModel
                {
                    ProductVariants = variantInfos,
                    SearchTerm = searchTerm ?? string.Empty,
                    CategoryId = categoryId,
                    BrandId = brandId,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    TotalItems = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants list");
                return new ProductVariantListViewModel
                {
                    ProductVariants = new List<ProductVariantDisplayInfo>(),
                    SearchTerm = searchTerm ?? string.Empty,
                    CategoryId = categoryId,
                    BrandId = brandId,
                    CurrentPage = page,
                    TotalPages = 0,
                    PageSize = pageSize,
                    TotalItems = 0
                };
            }
        }

        public async Task<IEnumerable<ProductVariantDisplayInfo>> GetFeaturedProductVariantsAsync(int count = 8)
        {
            try
            {
                // Get variants with stock, ordered by creation date (newest first)
                var variants = await _productRepository.GetFeaturedProductVariantsAsync(count);

                return variants.Select(v => new ProductVariantDisplayInfo
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    ProductName = v.Product?.Name ?? "N/A",
                    ProductDescription = v.Product?.Description ?? string.Empty,
                    CategoryName = v.Product?.Category?.Name ?? "N/A",
                    BrandName = v.Product?.Brand?.Name ?? "N/A",
                    Color = v.Color ?? string.Empty,
                    Size = v.Size ?? string.Empty,
                    ImageUrl = !string.IsNullOrEmpty(v.ImageUrl) ? v.ImageUrl : "/images/no-image.svg",
                    Price = v.Price,
                    StockQuantity = v.AvailableQuantity,
                    HasActiveDiscount = v.HasActiveDiscount,
                    DiscountName = v.GetActiveDiscount()?.Name,
                    DiscountPercentage = v.DiscountPercentage,
                    DiscountAmount = v.DiscountAmount,
                    DiscountedPrice = v.DiscountedPrice,
                    CreatedDate = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting featured product variants");
                return new List<ProductVariantDisplayInfo>();
            }
        }

        public async Task<IEnumerable<ProductVariantDisplayInfo>> GetDiscountedProductVariantsAsync(int page = 1, int pageSize = 12)
        {
            try
            {
                var variants = await _productRepository.GetDiscountedProductVariantsAsync(page, pageSize);

                return variants.Select(v => new ProductVariantDisplayInfo
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    ProductName = v.Product?.Name ?? "N/A",
                    ProductDescription = v.Product?.Description ?? string.Empty,
                    CategoryName = v.Product?.Category?.Name ?? "N/A",
                    BrandName = v.Product?.Brand?.Name ?? "N/A",
                    Color = v.Color ?? string.Empty,
                    Size = v.Size ?? string.Empty,
                    ImageUrl = !string.IsNullOrEmpty(v.ImageUrl) ? v.ImageUrl : "/images/no-image.svg",
                    Price = v.Price,
                    StockQuantity = v.AvailableQuantity,
                    HasActiveDiscount = v.HasActiveDiscount,
                    DiscountName = v.GetActiveDiscount()?.Name,
                    DiscountPercentage = v.DiscountPercentage,
                    DiscountAmount = v.DiscountAmount,
                    DiscountedPrice = v.DiscountedPrice,
                    CreatedDate = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discounted product variants");
                return new List<ProductVariantDisplayInfo>();
            }
        }
    }
}