using System.ComponentModel.DataAnnotations;

namespace ShoesEcommerce.ViewModels.Stock
{
    // ===== T?N KHO (INVENTORY) VIEW MODELS =====
    public class InventoryListViewModel
    {
        public IEnumerable<InventoryItemViewModel> Items { get; set; } = new List<InventoryItemViewModel>();
        public string SearchTerm { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public InventoryStatsViewModel Stats { get; set; } = new();
    }

    public class InventoryItemViewModel
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int TotalQuantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
    }

    public class InventoryStatsViewModel
    {
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public decimal TotalValue { get; set; }
        public int TotalQuantity { get; set; }
    }

    // ===== KI?M KHO (STOCK AUDIT) VIEW MODELS =====
    public class StockAuditListViewModel
    {
        public IEnumerable<StockAuditItemViewModel> Items { get; set; } = new List<StockAuditItemViewModel>();
        public string SearchTerm { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public StockAuditStatsViewModel Stats { get; set; } = new();
    }

    public class StockAuditItemViewModel
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int SystemQuantity { get; set; }
        public int ActualQuantity { get; set; }
        public int Difference { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastAuditDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class PerformAuditViewModel
    {
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int SystemQuantity { get; set; }
        
        [Required(ErrorMessage = "S? l??ng th?c t? là b?t bu?c")]
        [Range(0, int.MaxValue, ErrorMessage = "S? l??ng th?c t? ph?i >= 0")]
        public int ActualQuantity { get; set; }
        
        [StringLength(500, ErrorMessage = "Ghi chú không ???c quá 500 ký t?")]
        public string Notes { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Tên ng??i ki?m tra là b?t bu?c")]
        public string AuditedBy { get; set; } = string.Empty;
    }

    public class StockAuditStatsViewModel
    {
        public int TotalAudited { get; set; }
        public int CorrectCount { get; set; }
        public int OverCount { get; set; }
        public int UnderCount { get; set; }
        public decimal AccuracyRate { get; set; }
    }

    // ===== NH?P HÀNG (STOCK IMPORT) VIEW MODELS =====
    public class StockImportListViewModel
    {
        public IEnumerable<StockImportItemViewModel> Items { get; set; } = new List<StockImportItemViewModel>();
        public string SearchTerm { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalItems { get; set; }
        public IEnumerable<SupplierSelectViewModel> Suppliers { get; set; } = new List<SupplierSelectViewModel>();
    }

    public class StockImportItemViewModel
    {
        public int Id { get; set; }
        public string ImportId { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int QuantityReceived { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsProcessed { get; set; }
    }

    public class CreateStockImportViewModel
    {
        [Required(ErrorMessage = "S?n ph?m là b?t bu?c")]
        public int ProductVariantId { get; set; }
        
        [Required(ErrorMessage = "Nhà cung c?p là b?t bu?c")]
        public int SupplierId { get; set; }
        
        [Required(ErrorMessage = "S? l??ng là b?t bu?c")]
        [Range(1, int.MaxValue, ErrorMessage = "S? l??ng ph?i > 0")]
        public int QuantityReceived { get; set; }
        
        [Required(ErrorMessage = "Giá nh?p là b?t bu?c")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá nh?p ph?i > 0")]
        public decimal UnitCost { get; set; }
        
        [StringLength(50, ErrorMessage = "S? lô không ???c quá 50 ký t?")]
        public string BatchNumber { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Ghi chú không ???c quá 500 ký t?")]
        public string Notes { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Tên ng??i nh?n là b?t bu?c")]
        public string ReceivedBy { get; set; } = string.Empty;

        // For dropdowns
        public IEnumerable<ProductVariantSelectViewModel> ProductVariants { get; set; } = new List<ProductVariantSelectViewModel>();
        public IEnumerable<SupplierSelectViewModel> Suppliers { get; set; } = new List<SupplierSelectViewModel>();
    }

    public class EditStockImportViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "S? l??ng là b?t bu?c")]
        [Range(1, int.MaxValue, ErrorMessage = "S? l??ng ph?i > 0")]
        public int QuantityReceived { get; set; }
        
        [Required(ErrorMessage = "Giá nh?p là b?t bu?c")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá nh?p ph?i > 0")]
        public decimal UnitCost { get; set; }
        
        [StringLength(50, ErrorMessage = "S? lô không ???c quá 50 ký t?")]
        public string BatchNumber { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Ghi chú không ???c quá 500 ký t?")]
        public string Notes { get; set; } = string.Empty;

        // Display info
        public string ProductName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public bool IsProcessed { get; set; }
    }

    // ===== SHARED VIEW MODELS =====
    public class ProductVariantSelectViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CurrentStock { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class SupplierSelectViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
    }

    // ===== AJAX RESPONSE MODELS =====
    public class AjaxResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
    }

    public class StockUpdateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int NewAvailableQuantity { get; set; }
        public int NewTotalQuantity { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}