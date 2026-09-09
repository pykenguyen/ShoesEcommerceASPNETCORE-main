using ShoesEcommerce.Models.Promotions;
using System.ComponentModel.DataAnnotations;

namespace ShoesEcommerce.ViewModels.Promotion
{
    public class DiscountListViewModel
    {
        public IEnumerable<DiscountInfo> Discounts { get; set; } = new List<DiscountInfo>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public DiscountType? TypeFilter { get; set; }
    }

    public class DiscountInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DiscountType Type { get; set; }
        public decimal? PercentageValue { get; set; }
        public decimal? FixedValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public int CurrentUsageCount { get; set; }
        public int? MaxUsageCount { get; set; }
        public DiscountScope Scope { get; set; }
        
        public string DisplayValue => Type == DiscountType.Percentage 
            ? $"{PercentageValue}%" 
            : $"{FixedValue:C0}";
        
        public string StatusDisplay => IsActive switch
        {
            true when DateTime.Now < StartDate => "Chưa bắt đầu",
            true when DateTime.Now > EndDate => "Đã hết hạn", 
            true => "Đang hoạt động",
            false => "Ngừng hoạt động"
        };
    }

    public class CreateDiscountViewModel
    {
        [Required(ErrorMessage = "Tên khuyến mãi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên không được quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được quá 500 ký tự")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã khuyến mãi là bắt buộc")]
        [StringLength(50, ErrorMessage = "Mã không được quá 50 ký tự")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại khuyến mãi là bắt buộc")]
        public DiscountType Type { get; set; }

        [Range(0.01, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0.01% đến 100%")]
        public decimal? PercentageValue { get; set; }

        [Range(1000, double.MaxValue, ErrorMessage = "Số tiền giảm tối thiểu 1,000 VND")]
        public decimal? FixedValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu không được âm")]
        public decimal? MinimumOrderValue { get; set; }

        [Range(1000, double.MaxValue, ErrorMessage = "Số tiền giảm tối đa tối thiểu 1,000 VND")]
        public decimal? MaximumDiscountAmount { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        [Range(1, int.MaxValue, ErrorMessage = "Số lần sử dụng tối đa phải lớn hơn 0")]
        public int? MaxUsageCount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lần sử dụng tối đa mỗi khách hàng phải lớn hơn 0")]
        public int? MaxUsagePerCustomer { get; set; }

        [Required(ErrorMessage = "Phạm vi áp dụng là bắt buộc")]
        public DiscountScope Scope { get; set; }

        public List<int> SelectedProductIds { get; set; } = new List<int>();
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();
    }

    public class EditDiscountViewModel : CreateDiscountViewModel
    {
        public int Id { get; set; }
        public int CurrentUsageCount { get; set; }
    }

    public class FeaturedDiscountViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DiscountType Type { get; set; }
        public decimal? PercentageValue { get; set; }
        public decimal? FixedValue { get; set; }
        public DateTime EndDate { get; set; }
        
        public string DisplayValue => Type == DiscountType.Percentage 
            ? $"Giảm {PercentageValue}%" 
            : $"Giảm {FixedValue:C0}";
    }
}