using ShoesEcommerce.ViewModels.Promotion;
using System.ComponentModel.DataAnnotations;

namespace ShoesEcommerce.ViewModels.Product
{
    // Product ViewModels
    public class ProductListViewModel
    {
        public IEnumerable<ProductInfo> Products { get; set; } = new List<ProductInfo>();
        public IEnumerable<FeaturedDiscountViewModel> FeaturedDiscounts { get; set; } = new List<FeaturedDiscountViewModel>(); // ✅ ADD
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; } // ✅ ADD: Match existing usage
        public int? BrandId { get; set; } // ✅ ADD: Match existing usage
        public int? CategoryFilter { get; set; }
        public int? BrandFilter { get; set; }
        public int PageSize { get; set; } = 10; // ✅ ADD: Match existing usage
        public int TotalCount { get; set; } // ✅ ADD: Match existing usage
        public bool ShowDiscountsOnly { get; set; } = false; // ✅ ADD: Filter for discounted products
    }

    public class ProductInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal Price { get; set; } // ✅ ADD: Match existing usage (will use MinPrice)
        public string ImageUrl { get; set; } = string.Empty;
        public int VariantCount { get; set; }
        public bool IsInStock { get; set; }
        public int TotalStock { get; set; } // ✅ ADD: Match existing usage
        public DateTime CreatedDate { get; set; } = DateTime.Now; // ✅ ADD: Match existing usage
        public bool IsActive { get; set; } = true; // ✅ ADD: Match existing usage

        // ✅ DISCOUNT PROPERTIES
        public bool HasActiveDiscount { get; set; }
        public string? DiscountName { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal DiscountedMinPrice { get; set; }
        public decimal DiscountedMaxPrice { get; set; }
    }

    public class CreateProductViewModel
    {
        [Required(ErrorMessage = "Tên s?n ph?m là b?t bu?c")]
        [StringLength(200, ErrorMessage = "Tên s?n ph?m không ???c quá 200 ký t?")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô t? không ???c quá 1000 ký t?")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Danh m?c là b?t bu?c")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Th??ng hi?u là b?t bu?c")]
        public int BrandId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class EditProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên s?n ph?m là b?t bu?c")]
        [StringLength(200, ErrorMessage = "Tên s?n ph?m không ???c quá 200 ký t?")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô t? không ???c quá 1000 ký t?")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Danh m?c là b?t bu?c")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Th??ng hi?u là b?t bu?c")]
        public int BrandId { get; set; }

        public bool IsActive { get; set; }
    }

    // Product Variant ViewModels
    public class ProductVariantInfo
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        
        // ? RENAMED: Maps to AvailableQuantity from Stock entity
        public int StockQuantity { get; set; } // For display purposes, maps to AvailableQuantity
    }

    public class CreateProductVariantViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Màu sắc là bắt buộc")]
        [StringLength(50, ErrorMessage = "Màu sắc không được quá 50 ký tự")]
        public string Color { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kích thước là bắt buộc")]
        [StringLength(20, ErrorMessage = "Kích thước không được quá 20 ký tự")]
        public string Size { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0 VNĐ")]
        public decimal Price { get; set; }

        [StringLength(500, ErrorMessage = "URL hình ảnh không được quá 500 ký tự")]
        public string? ImageUrl { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải không âm")]
        public int InitialStockQuantity { get; set; }

        // File upload properties
        [Display(Name = "Hình ảnh phiên bản")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Sử dụng URL thay vì tải lên")]
        public bool UseImageUrl { get; set; } = false;
    }

    public class EditProductVariantViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Màu sắc là bắt buộc")]
        [StringLength(50, ErrorMessage = "Màu sắc không được quá 50 ký tự")]
        public string Color { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kích thước là bắt buộc")]
        [StringLength(20, ErrorMessage = "Kích thước không được quá 20 ký tự")]
        public string Size { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0 VNĐ")]
        public decimal Price { get; set; }

        [StringLength(500, ErrorMessage = "URL hình ảnh không được quá 500 ký tự")]
        public string? ImageUrl { get; set; } // Nullable - not required

        // File upload properties for editing
        [Display(Name = "Hình ảnh mới")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Sử dụng URL thay vì tải lên")]
        public bool UseImageUrl { get; set; } = false;

        [Display(Name = "Giữ hình ảnh hiện tại")]
        public bool KeepCurrentImage { get; set; } = true;

        // Current image info for display
        public string? CurrentImageUrl { get; set; }
    }

    // Category ViewModels
    public class CategoryInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }

    public class CreateCategoryViewModel
    {
        [Required(ErrorMessage = "Tên danh m?c là b?t bu?c")]
        [StringLength(100, ErrorMessage = "Tên danh m?c không ???c quá 100 ký t?")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô t? không ???c quá 500 ký t?")]
        public string Description { get; set; } = string.Empty;
    }

    public class EditCategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh m?c là b?t bu?c")]
        [StringLength(100, ErrorMessage = "Tên danh m?c không ???c quá 100 ký t?")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô t? không ???c quá 500 ký t?")]
        public string Description { get; set; } = string.Empty;
    }

    // Brand ViewModels
    public class BrandInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }

    public class CreateBrandViewModel
    {
        [Required(ErrorMessage = "Tên th??ng hi?u là b?t bu?c")]
        [StringLength(100, ErrorMessage = "Tên th??ng hi?u không ???c quá 100 ký t?")]
        public string Name { get; set; } = string.Empty;
    }

    public class EditBrandViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên th??ng hi?u là b?t bu?c")]
        [StringLength(100, ErrorMessage = "Tên th??ng hi?u không ???c quá 100 ký t?")]
        public string Name { get; set; } = string.Empty;
    }

    // Supplier ViewModels
    public class SupplierInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public int StockEntryCount { get; set; } = 0;
    }

    public class CreateSupplierViewModel
    {
        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên nhà cung cấp không được quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Thông tin liên hệ không được quá 200 ký tự")]
        public string ContactInfo { get; set; } = string.Empty;
    }

    public class EditSupplierViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên nhà cung cấp không được quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Thông tin liên hệ không được quá 200 ký tự")]
        public string ContactInfo { get; set; } = string.Empty;
    }
}