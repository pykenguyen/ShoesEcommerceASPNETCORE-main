using ShoesEcommerce.Models.Stocks;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface IStockRepository
    {
        // ===== STOCK MANAGEMENT (T?n kho) =====
        Task<IEnumerable<Stock>> GetAllStocksAsync();
        Task<Stock?> GetStockByIdAsync(int id);
        Task<Stock?> GetStockByProductVariantIdAsync(int productVariantId);
        Task<IEnumerable<Stock>> GetStocksByStatusAsync(bool isLowStock, bool isOutOfStock);
        Task<Stock> CreateOrUpdateStockAsync(Stock stock);
        Task<bool> UpdateStockQuantityAsync(int productVariantId, int availableQuantity, int reservedQuantity, string updatedBy);
        Task<bool> DeleteStockAsync(int id);

        // ===== STOCK ENTRIES (Nh?p hàng) =====
        Task<IEnumerable<StockEntry>> GetAllStockEntriesAsync();
        Task<IEnumerable<StockEntry>> GetStockEntriesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<StockEntry>> GetStockEntriesBySupplierAsync(int supplierId);
        Task<IEnumerable<StockEntry>> GetUnprocessedStockEntriesAsync();
        Task<StockEntry?> GetStockEntryByIdAsync(int id);
        Task<StockEntry> CreateStockEntryAsync(StockEntry stockEntry);
        Task<bool> UpdateStockEntryAsync(StockEntry stockEntry);
        Task<bool> ProcessStockEntryAsync(int stockEntryId, string processedBy);
        Task<bool> DeleteStockEntryAsync(int id);

        // ===== STOCK TRANSACTIONS (L?ch s?) =====
        Task<IEnumerable<StockTransaction>> GetAllStockTransactionsAsync();
        Task<IEnumerable<StockTransaction>> GetStockTransactionsByProductVariantAsync(int productVariantId);
        Task<IEnumerable<StockTransaction>> GetStockTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<StockTransaction>> GetStockTransactionsByTypeAsync(StockTransactionType type);
        Task<StockTransaction> CreateStockTransactionAsync(StockTransaction transaction);

        // ===== STOCK AUDIT (Ki?m kho) =====
        Task<IEnumerable<Stock>> GetStocksForAuditAsync();
        Task<bool> CreateStockAuditAsync(int productVariantId, int systemQuantity, int actualQuantity, string auditedBy, string notes);
        Task<IEnumerable<StockTransaction>> GetAuditHistoryAsync(DateTime? startDate = null, DateTime? endDate = null);

        // ===== ANALYTICS & REPORTS =====
        Task<decimal> GetTotalStockValueAsync();
        Task<int> GetTotalStockQuantityAsync();
        Task<int> GetLowStockCountAsync();
        Task<int> GetOutOfStockCountAsync();
        Task<IEnumerable<Stock>> GetTopStockMovementsAsync(int count = 10);
        Task<Dictionary<string, int>> GetStockSummaryByStatusAsync();
        Task<Dictionary<string, decimal>> GetStockValueBySupplierAsync();

        // ===== SEARCH & FILTER =====
        Task<IEnumerable<Stock>> SearchStocksAsync(string searchTerm);
        Task<IEnumerable<StockEntry>> SearchStockEntriesAsync(string searchTerm);
        Task<IEnumerable<Stock>> GetStocksByProductAsync(int productId);
    }
}