using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Stocks;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Repositories.Interfaces;

namespace ShoesEcommerce.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly AppDbContext _context;
        private readonly ILogger<StockService> _logger;

        public StockService(IStockRepository stockRepository, AppDbContext context, ILogger<StockService> logger)
        {
            _stockRepository = stockRepository;
            _context = context;
            _logger = logger;
        }

        // ===== EXISTING CORE OPERATIONS =====
        public async Task<bool> AddStockAsync(int productVariantId, int quantity, int supplierId, string receivedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create StockEntry
                var stockEntry = new StockEntry
                {
                    ProductVariantId = productVariantId,
                    SupplierId = supplierId,
                    QuantityReceived = quantity,
                    EntryDate = DateTime.UtcNow,
                    ReceivedBy = receivedBy,
                    IsProcessed = true
                };
                await _stockRepository.CreateStockEntryAsync(stockEntry);

                // 2. Update or Create Stock record
                var stock = await _stockRepository.GetStockByProductVariantIdAsync(productVariantId);
                if (stock == null)
                {
                    stock = new Stock
                    {
                        ProductVariantId = productVariantId,
                        AvailableQuantity = quantity,
                        ReservedQuantity = 0,
                        LastUpdated = DateTime.UtcNow,
                        LastUpdatedBy = receivedBy
                    };
                }
                else
                {
                    var availableBefore = stock.AvailableQuantity;
                    stock.AvailableQuantity += quantity;
                    stock.LastUpdated = DateTime.UtcNow;
                    stock.LastUpdatedBy = receivedBy;

                    // Create transaction record
                    var stockTransaction = new StockTransaction
                    {
                        ProductVariantId = productVariantId,
                        Type = StockTransactionType.StockIn,
                        QuantityChange = quantity,
                        AvailableQuantityBefore = availableBefore,
                        AvailableQuantityAfter = stock.AvailableQuantity,
                        ReservedQuantityBefore = stock.ReservedQuantity,
                        ReservedQuantityAfter = stock.ReservedQuantity,
                        TransactionDate = DateTime.UtcNow,
                        Reason = "Stock received from supplier",
                        CreatedBy = receivedBy,
                        ReferenceType = "StockEntry",
                        ReferenceId = stockEntry.Id
                    };
                    await _stockRepository.CreateStockTransactionAsync(stockTransaction);
                }

                await _stockRepository.CreateOrUpdateStockAsync(stock);
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding stock for ProductVariant {ProductVariantId}", productVariantId);
                return false;
            }
        }

        public async Task<bool> ReserveStockAsync(int productVariantId, int quantity, string reason)
        {
            try
            {
                var stock = await _stockRepository.GetStockByProductVariantIdAsync(productVariantId);
                if (stock == null || stock.AvailableQuantity < quantity)
                    return false;

                var availableBefore = stock.AvailableQuantity;
                var reservedBefore = stock.ReservedQuantity;

                var success = await _stockRepository.UpdateStockQuantityAsync(
                    productVariantId, 
                    stock.AvailableQuantity - quantity, 
                    stock.ReservedQuantity + quantity, 
                    "System");

                if (success)
                {
                    var transaction = new StockTransaction
                    {
                        ProductVariantId = productVariantId,
                        Type = StockTransactionType.Reserve,
                        QuantityChange = -quantity,
                        AvailableQuantityBefore = availableBefore,
                        AvailableQuantityAfter = availableBefore - quantity,
                        ReservedQuantityBefore = reservedBefore,
                        ReservedQuantityAfter = reservedBefore + quantity,
                        TransactionDate = DateTime.UtcNow,
                        Reason = reason,
                        CreatedBy = "System"
                    };
                    await _stockRepository.CreateStockTransactionAsync(transaction);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving stock for ProductVariant {ProductVariantId}", productVariantId);
                return false;
            }
        }

        public async Task<bool> ReleaseStockAsync(int productVariantId, int quantity, string reason)
        {
            try
            {
                var stock = await _stockRepository.GetStockByProductVariantIdAsync(productVariantId);
                if (stock == null || stock.ReservedQuantity < quantity)
                    return false;

                var availableBefore = stock.AvailableQuantity;
                var reservedBefore = stock.ReservedQuantity;

                var success = await _stockRepository.UpdateStockQuantityAsync(
                    productVariantId, 
                    stock.AvailableQuantity + quantity, 
                    stock.ReservedQuantity - quantity, 
                    "System");

                if (success)
                {
                    var transaction = new StockTransaction
                    {
                        ProductVariantId = productVariantId,
                        Type = StockTransactionType.Release,
                        QuantityChange = quantity,
                        AvailableQuantityBefore = availableBefore,
                        AvailableQuantityAfter = availableBefore + quantity,
                        ReservedQuantityBefore = reservedBefore,
                        ReservedQuantityAfter = reservedBefore - quantity,
                        TransactionDate = DateTime.UtcNow,
                        Reason = reason,
                        CreatedBy = "System"
                    };
                    await _stockRepository.CreateStockTransactionAsync(transaction);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing stock for ProductVariant {ProductVariantId}", productVariantId);
                return false;
            }
        }

        public async Task<bool> RemoveStockAsync(int productVariantId, int quantity, string reason)
        {
            try
            {
                var stock = await _stockRepository.GetStockByProductVariantIdAsync(productVariantId);
                if (stock == null || stock.AvailableQuantity < quantity)
                    return false;

                var availableBefore = stock.AvailableQuantity;

                var success = await _stockRepository.UpdateStockQuantityAsync(
                    productVariantId, 
                    stock.AvailableQuantity - quantity, 
                    stock.ReservedQuantity, 
                    "System");

                if (success)
                {
                    var transaction = new StockTransaction
                    {
                        ProductVariantId = productVariantId,
                        Type = StockTransactionType.StockOut,
                        QuantityChange = -quantity,
                        AvailableQuantityBefore = availableBefore,
                        AvailableQuantityAfter = availableBefore - quantity,
                        ReservedQuantityBefore = stock.ReservedQuantity,
                        ReservedQuantityAfter = stock.ReservedQuantity,
                        TransactionDate = DateTime.UtcNow,
                        Reason = reason,
                        CreatedBy = "System"
                    };
                    await _stockRepository.CreateStockTransactionAsync(transaction);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing stock for ProductVariant {ProductVariantId}", productVariantId);
                return false;
            }
        }

        public async Task<bool> AdjustStockAsync(int productVariantId, int newQuantity, string reason, string adjustedBy)
        {
            try
            {
                var stock = await _stockRepository.GetStockByProductVariantIdAsync(productVariantId);
                var availableBefore = stock?.AvailableQuantity ?? 0;
                var reservedBefore = stock?.ReservedQuantity ?? 0;
                var quantityChange = newQuantity - availableBefore;

                var success = await _stockRepository.UpdateStockQuantityAsync(
                    productVariantId, 
                    newQuantity, 
                    reservedBefore, 
                    adjustedBy);

                if (success)
                {
                    var transaction = new StockTransaction
                    {
                        ProductVariantId = productVariantId,
                        Type = StockTransactionType.Adjustment,
                        QuantityChange = quantityChange,
                        AvailableQuantityBefore = availableBefore,
                        AvailableQuantityAfter = newQuantity,
                        ReservedQuantityBefore = reservedBefore,
                        ReservedQuantityAfter = reservedBefore,
                        TransactionDate = DateTime.UtcNow,
                        Reason = reason,
                        CreatedBy = adjustedBy
                    };
                    await _stockRepository.CreateStockTransactionAsync(transaction);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting stock for ProductVariant {ProductVariantId}", productVariantId);
                return false;
            }
        }

        public async Task<Stock?> GetCurrentStockAsync(int productVariantId)
        {
            try
            {
                return await _stockRepository.GetStockByProductVariantIdAsync(productVariantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current stock for ProductVariant {ProductVariantId}", productVariantId);
                return null;
            }
        }

        public async Task<IEnumerable<StockTransaction>> GetStockHistoryAsync(int productVariantId)
        {
            try
            {
                return await _stockRepository.GetStockTransactionsByProductVariantAsync(productVariantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock history for ProductVariant {ProductVariantId}", productVariantId);
                return new List<StockTransaction>();
            }
        }

        // ===== STOCK MANAGEMENT (T?n kho) =====
        public async Task<IEnumerable<Stock>> GetAllInventoryAsync()
        {
            try
            {
                return await _stockRepository.GetAllStocksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all inventory");
                return new List<Stock>();
            }
        }

        public async Task<IEnumerable<Stock>> SearchInventoryAsync(string searchTerm)
        {
            try
            {
                return await _stockRepository.SearchStocksAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching inventory with term: {SearchTerm}", searchTerm);
                return new List<Stock>();
            }
        }

        public async Task<IEnumerable<Stock>> GetInventoryByStatusAsync(string status)
        {
            try
            {
                return status.ToLower() switch
                {
                    "low-stock" => await _stockRepository.GetStocksByStatusAsync(true, false),
                    "out-of-stock" => await _stockRepository.GetStocksByStatusAsync(false, true),
                    "in-stock" => await _stockRepository.GetStocksByStatusAsync(false, false),
                    _ => await _stockRepository.GetAllStocksAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory by status: {Status}", status);
                return new List<Stock>();
            }
        }

        public async Task<IEnumerable<Stock>> GetInventoryByProductAsync(int productId)
        {
            try
            {
                return await _stockRepository.GetStocksByProductAsync(productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory by product: {ProductId}", productId);
                return new List<Stock>();
            }
        }

        public async Task<Dictionary<string, int>> GetInventoryStatsAsync()
        {
            try
            {
                return await _stockRepository.GetStockSummaryByStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory stats");
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, decimal>> GetInventoryValueAsync()
        {
            try
            {
                var totalValue = await _stockRepository.GetTotalStockValueAsync();
                var valueBySupplier = await _stockRepository.GetStockValueBySupplierAsync();
                
                var result = new Dictionary<string, decimal> { ["Total"] = totalValue };
                foreach (var item in valueBySupplier)
                {
                    result[item.Key] = item.Value;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory value");
                return new Dictionary<string, decimal>();
            }
        }

        // ===== STOCK ENTRY (Nh?p hàng) =====
        public async Task<IEnumerable<StockEntry>> GetAllStockEntriesAsync()
        {
            try
            {
                return await _stockRepository.GetAllStockEntriesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all stock entries");
                return new List<StockEntry>();
            }
        }

        public async Task<IEnumerable<StockEntry>> GetStockEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _stockRepository.GetStockEntriesByDateRangeAsync(startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock entries by date range");
                return new List<StockEntry>();
            }
        }

        public async Task<IEnumerable<StockEntry>> GetStockEntriesBySupplierAsync(int supplierId)
        {
            try
            {
                return await _stockRepository.GetStockEntriesBySupplierAsync(supplierId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock entries by supplier: {SupplierId}", supplierId);
                return new List<StockEntry>();
            }
        }

        public async Task<IEnumerable<StockEntry>> GetUnprocessedEntriesAsync()
        {
            try
            {
                return await _stockRepository.GetUnprocessedStockEntriesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unprocessed entries");
                return new List<StockEntry>();
            }
        }

        public async Task<StockEntry?> GetStockEntryByIdAsync(int id)
        {
            try
            {
                return await _stockRepository.GetStockEntryByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock entry by id: {Id}", id);
                return null;
            }
        }

        public async Task<StockEntry> CreateStockEntryAsync(int productVariantId, int supplierId, int quantity, decimal unitCost, string batchNumber, string notes, string receivedBy)
        {
            try
            {
                _logger.LogInformation("=== CREATE STOCK ENTRY SERVICE STARTED ===");
                _logger.LogInformation("Parameters - ProductVariantId: {ProductVariantId}, SupplierId: {SupplierId}, Quantity: {Quantity}, UnitCost: {UnitCost}, BatchNumber: {BatchNumber}, ReceivedBy: {ReceivedBy}", 
                    productVariantId, supplierId, quantity, unitCost, batchNumber ?? "NULL", receivedBy);

                var stockEntry = new StockEntry
                {
                    ProductVariantId = productVariantId,
                    SupplierId = supplierId,
                    QuantityReceived = quantity,
                    UnitCost = unitCost,
                    BatchNumber = batchNumber ?? string.Empty,
                    Notes = notes ?? string.Empty,
                    ReceivedBy = receivedBy,
                    EntryDate = DateTime.UtcNow, // Use UTC for PostgreSQL
                    IsProcessed = false
                };

                _logger.LogDebug("StockEntry object created. Calling repository...");

                var createdEntry = await _stockRepository.CreateStockEntryAsync(stockEntry);

                _logger.LogInformation("Stock entry created successfully in database with ID: {StockEntryId}", createdEntry.Id);

                return createdEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR in CreateStockEntryAsync. ProductVariantId: {ProductVariantId}, SupplierId: {SupplierId}. Exception: {ExceptionMessage}, InnerException: {InnerException}", 
                    productVariantId, supplierId, ex.Message, ex.InnerException?.Message ?? "NONE");
                throw;
            }
        }

        public async Task<bool> ProcessStockEntryAsync(int stockEntryId, string processedBy)
        {
            // 1. Get StockEntry
            var entry = await _stockRepository.GetStockEntryByIdAsync(stockEntryId);
            if (entry == null || entry.IsProcessed)
                return false;

            // 2. Update Stock
            var stock = await _stockRepository.GetStockByProductVariantIdAsync(entry.ProductVariantId);
            int beforeQty = stock?.AvailableQuantity ?? 0;
            if (stock == null)
            {
                stock = new Stock
                {
                    ProductVariantId = entry.ProductVariantId,
                    AvailableQuantity = entry.QuantityReceived,
                    ReservedQuantity = 0,
                    LastUpdated = DateTime.UtcNow,
                    LastUpdatedBy = processedBy
                };
                await _stockRepository.CreateOrUpdateStockAsync(stock);
            }
            else
            {
                stock.AvailableQuantity += entry.QuantityReceived;
                stock.LastUpdated = DateTime.UtcNow;
                stock.LastUpdatedBy = processedBy;
                await _stockRepository.CreateOrUpdateStockAsync(stock);
            }

            // 3. Create StockTransaction
            var transaction = new StockTransaction
            {
                ProductVariantId = entry.ProductVariantId,
                Type = StockTransactionType.StockIn,
                QuantityChange = entry.QuantityReceived,
                AvailableQuantityBefore = beforeQty,
                AvailableQuantityAfter = stock.AvailableQuantity,
                ReservedQuantityBefore = stock.ReservedQuantity,
                ReservedQuantityAfter = stock.ReservedQuantity,
                TransactionDate = DateTime.UtcNow,
                Reason = "Nh?p kho t? phi?u nh?p",
                Notes = entry.Notes,
                CreatedBy = processedBy,
                ReferenceType = "StockEntry",
                ReferenceId = entry.Id
            };
            await _stockRepository.CreateStockTransactionAsync(transaction);

            // 4. Mark entry as processed
            entry.IsProcessed = true;
            await _stockRepository.UpdateStockEntryAsync(entry);
            return true;
        }

        public async Task<bool> PerformStockAuditAsync(int productVariantId, int actualQuantity, string auditedBy, string notes)
        {
            try
            {
                _logger.LogInformation("Performing stock audit for ProductVariant {ProductVariantId}. Actual: {ActualQuantity}, By: {AuditedBy}", 
                    productVariantId, actualQuantity, auditedBy);

                var stock = await _stockRepository.GetStockByProductVariantIdAsync(productVariantId);
                if (stock == null)
                {
                    _logger.LogWarning("Stock not found for ProductVariant {ProductVariantId}", productVariantId);
                    return false;
                }

                int beforeQty = stock.AvailableQuantity;
                int diff = actualQuantity - beforeQty;

                _logger.LogDebug("Stock audit calculation - Before: {Before}, After: {After}, Difference: {Diff}", 
                    beforeQty, actualQuantity, diff);

                // Update stock quantity
                stock.AvailableQuantity = actualQuantity;
                stock.LastUpdated = DateTime.UtcNow; // Use UTC for PostgreSQL
                stock.LastUpdatedBy = auditedBy;

                await _stockRepository.CreateOrUpdateStockAsync(stock);

                // Create audit transaction
                var transaction = new StockTransaction
                {
                    ProductVariantId = productVariantId,
                    Type = StockTransactionType.Adjustment,
                    QuantityChange = diff,
                    AvailableQuantityBefore = beforeQty,
                    AvailableQuantityAfter = actualQuantity,
                    ReservedQuantityBefore = stock.ReservedQuantity,
                    ReservedQuantityAfter = stock.ReservedQuantity,
                    TransactionDate = DateTime.UtcNow, // Use UTC for PostgreSQL
                    Reason = "Ki?m kho - ?i?u ch?nh t?n kho th?c t?",
                    Notes = notes ?? string.Empty,
                    CreatedBy = auditedBy,
                    ReferenceType = "StockAudit"
                };

                await _stockRepository.CreateStockTransactionAsync(transaction);

                _logger.LogInformation("Stock audit completed successfully for ProductVariant {ProductVariantId}. Difference: {Diff}", 
                    productVariantId, diff);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing stock audit for ProductVariant {ProductVariantId}. Error: {ErrorMessage}", 
                    productVariantId, ex.Message);
                throw; // Re-throw to allow controller to handle
            }
        }

        public async Task<bool> UpdateStockEntryAsync(int id, int quantity, decimal unitCost, string batchNumber, string notes)
        {
            try
            {
                var stockEntry = await _stockRepository.GetStockEntryByIdAsync(id);
                if (stockEntry == null || stockEntry.IsProcessed)
                    return false;

                stockEntry.QuantityReceived = quantity;
                stockEntry.UnitCost = unitCost;
                stockEntry.BatchNumber = batchNumber;
                stockEntry.Notes = notes;

                return await _stockRepository.UpdateStockEntryAsync(stockEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock entry: {Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteStockEntryAsync(int id)
        {
            try
            {
                return await _stockRepository.DeleteStockEntryAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting stock entry: {Id}", id);
                return false;
            }
        }

        public async Task<IEnumerable<StockEntry>> SearchStockEntriesAsync(string searchTerm)
        {
            try
            {
                return await _stockRepository.SearchStockEntriesAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching stock entries with term: {SearchTerm}", searchTerm);
                return new List<StockEntry>();
            }
        }

        // ===== STOCK AUDIT (Ki?m kho) =====
        public async Task<IEnumerable<Stock>> GetStocksForAuditAsync()
        {
            try
            {
                return await _stockRepository.GetStocksForAuditAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stocks for audit");
                return new List<Stock>();
            }
        }

        public async Task<IEnumerable<StockTransaction>> GetAuditHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                return await _stockRepository.GetAuditHistoryAsync(startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit history");
                return new List<StockTransaction>();
            }
        }

        public async Task<Dictionary<string, object>> GetAuditSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var auditHistory = await GetAuditHistoryAsync(startDate, endDate);
                var totalAudits = auditHistory.Count();
                var totalAdjustments = auditHistory.Sum(ah => Math.Abs(ah.QuantityChange));
                var positiveAdjustments = auditHistory.Where(ah => ah.QuantityChange > 0).Sum(ah => ah.QuantityChange);
                var negativeAdjustments = auditHistory.Where(ah => ah.QuantityChange < 0).Sum(ah => Math.Abs(ah.QuantityChange));

                return new Dictionary<string, object>
                {
                    ["TotalAudits"] = totalAudits,
                    ["TotalAdjustments"] = totalAdjustments,
                    ["PositiveAdjustments"] = positiveAdjustments,
                    ["NegativeAdjustments"] = negativeAdjustments,
                    ["AuditAccuracy"] = totalAudits > 0 ? Math.Round((double)(totalAudits - auditHistory.Count(ah => ah.QuantityChange != 0)) / totalAudits * 100, 2) : 100
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit summary");
                return new Dictionary<string, object>();
            }
        }

        // ===== REPORTS & ANALYTICS =====
        public async Task<decimal> GetTotalStockValueAsync()
        {
            try
            {
                return await _stockRepository.GetTotalStockValueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total stock value");
                return 0;
            }
        }

        public async Task<int> GetTotalStockQuantityAsync()
        {
            try
            {
                return await _stockRepository.GetTotalStockQuantityAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total stock quantity");
                return 0;
            }
        }

        public async Task<IEnumerable<Stock>> GetTopMovingStocksAsync(int count = 10)
        {
            try
            {
                return await _stockRepository.GetTopStockMovementsAsync(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top moving stocks");
                return new List<Stock>();
            }
        }

        public async Task<Dictionary<string, decimal>> GetStockValueBySupplierAsync()
        {
            try
            {
                return await _stockRepository.GetStockValueBySupplierAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock value by supplier");
                return new Dictionary<string, decimal>();
            }
        }

        public async Task<Dictionary<string, int>> GetStockMovementsByTypeAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var transactions = await _stockRepository.GetStockTransactionsByDateRangeAsync(
                    startDate ?? DateTime.Now.AddMonths(-1), 
                    endDate ?? DateTime.Now);

                return transactions.GroupBy(t => t.Type.ToString())
                                 .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock movements by type");
                return new Dictionary<string, int>();
            }
        }

        // ===== SUPPLIERS & PRODUCT VARIANTS =====
        public async Task<IEnumerable<object>> GetSuppliersForDropdownAsync()
        {
            try
            {
                return await _context.Suppliers
                    .Select(s => new { 
                        Id = s.Id, 
                        Name = s.Name, 
                        ContactInfo = s.ContactInfo 
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suppliers for dropdown");
                return new List<object>();
            }
        }

        public async Task<IEnumerable<object>> GetProductVariantsForDropdownAsync()
        {
            try
            {
                return await _context.ProductVariants
                    .Include(pv => pv.Product)
                        .ThenInclude(p => p.Category)
                    .Include(pv => pv.Product)
                        .ThenInclude(p => p.Brand)
                    .Select(pv => new {
                        Id = pv.Id,
                        DisplayName = $"{pv.Product.Name} - {pv.Color} - {pv.Size}",
                        ProductName = pv.Product.Name,
                        Color = pv.Color,
                        Size = pv.Size,
                        Price = pv.Price,
                        CurrentStock = _context.Stocks
                            .Where(s => s.ProductVariantId == pv.Id)
                            .Select(s => s.AvailableQuantity)
                            .FirstOrDefault(),
                        ImageUrl = pv.ImageUrl
                    })
                    .OrderBy(pv => pv.ProductName)
                    .ThenBy(pv => pv.Color)
                    .ThenBy(pv => pv.Size)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product variants for dropdown");
                return new List<object>();
            }
        }
    }
}