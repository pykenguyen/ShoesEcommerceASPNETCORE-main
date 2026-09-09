using ShoesEcommerce.Models.Stocks;

namespace ShoesEcommerce.Services.Interfaces
{
    public interface IStockService
    {
        // ===== EXISTING CORE OPERATIONS =====
        Task<bool> AddStockAsync(int productVariantId, int quantity, int supplierId, string receivedBy);
        Task<bool> ReserveStockAsync(int productVariantId, int quantity, string reason);
        Task<bool> ReleaseStockAsync(int productVariantId, int quantity, string reason);
        Task<bool> RemoveStockAsync(int productVariantId, int quantity, string reason);
        Task<bool> AdjustStockAsync(int productVariantId, int newQuantity, string reason, string adjustedBy);
        Task<Stock?> GetCurrentStockAsync(int productVariantId);
        Task<IEnumerable<StockTransaction>> GetStockHistoryAsync(int productVariantId);

        // ===== STOCK MANAGEMENT (T?n kho) =====
        Task<IEnumerable<Stock>> GetAllInventoryAsync();
        Task<IEnumerable<Stock>> SearchInventoryAsync(string searchTerm);
        Task<IEnumerable<Stock>> GetInventoryByStatusAsync(string status); // "in-stock", "low-stock", "out-of-stock"
        Task<IEnumerable<Stock>> GetInventoryByProductAsync(int productId);
        Task<Dictionary<string, int>> GetInventoryStatsAsync();
        Task<Dictionary<string, decimal>> GetInventoryValueAsync();

        // ===== STOCK ENTRY (Nh?p hàng) =====
        Task<IEnumerable<StockEntry>> GetAllStockEntriesAsync();
        Task<IEnumerable<StockEntry>> GetStockEntriesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<StockEntry>> GetStockEntriesBySupplierAsync(int supplierId);
        Task<IEnumerable<StockEntry>> GetUnprocessedEntriesAsync();
        Task<StockEntry?> GetStockEntryByIdAsync(int id);
        Task<StockEntry> CreateStockEntryAsync(int productVariantId, int supplierId, int quantity, decimal unitCost, string batchNumber, string notes, string receivedBy);
        Task<bool> ProcessStockEntryAsync(int stockEntryId, string processedBy);
        Task<bool> UpdateStockEntryAsync(int id, int quantity, decimal unitCost, string batchNumber, string notes);
        Task<bool> DeleteStockEntryAsync(int id);
        Task<IEnumerable<StockEntry>> SearchStockEntriesAsync(string searchTerm);

        // ===== STOCK AUDIT (Ki?m kho) =====
        Task<IEnumerable<Stock>> GetStocksForAuditAsync();
        Task<bool> PerformStockAuditAsync(int productVariantId, int actualQuantity, string auditedBy, string notes);
        Task<IEnumerable<StockTransaction>> GetAuditHistoryAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, object>> GetAuditSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        // ===== REPORTS & ANALYTICS =====
        Task<decimal> GetTotalStockValueAsync();
        Task<int> GetTotalStockQuantityAsync();
        Task<IEnumerable<Stock>> GetTopMovingStocksAsync(int count = 10);
        Task<Dictionary<string, decimal>> GetStockValueBySupplierAsync();
        Task<Dictionary<string, int>> GetStockMovementsByTypeAsync(DateTime? startDate = null, DateTime? endDate = null);

        // ===== SUPPLIERS & PRODUCT VARIANTS =====
        Task<IEnumerable<object>> GetSuppliersForDropdownAsync();
        Task<IEnumerable<object>> GetProductVariantsForDropdownAsync();
    }
}