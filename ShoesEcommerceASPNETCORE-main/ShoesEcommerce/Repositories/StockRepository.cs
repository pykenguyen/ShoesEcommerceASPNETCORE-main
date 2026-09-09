using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Stocks;
using ShoesEcommerce.Repositories.Interfaces;

namespace ShoesEcommerce.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StockRepository> _logger;

        public StockRepository(AppDbContext context, ILogger<StockRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== STOCK MANAGEMENT (T?n kho) =====
        public async Task<IEnumerable<Stock>> GetAllStocksAsync()
        {
            try
            {
                return await _context.Stocks
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .OrderBy(s => s.ProductVariant.Product.Name)
                    .ThenBy(s => s.ProductVariant.Color)
                    .ThenBy(s => s.ProductVariant.Size)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all stocks");
                throw;
            }
        }

        public async Task<Stock?> GetStockByIdAsync(int id)
        {
            try
            {
                return await _context.Stocks
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock by id: {Id}", id);
                throw;
            }
        }

        public async Task<Stock?> GetStockByProductVariantIdAsync(int productVariantId)
        {
            try
            {
                return await _context.Stocks
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(s => s.ProductVariantId == productVariantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock by product variant id: {ProductVariantId}", productVariantId);
                throw;
            }
        }

        public async Task<IEnumerable<Stock>> GetStocksByStatusAsync(bool isLowStock, bool isOutOfStock)
        {
            try
            {
                var query = _context.Stocks
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .AsQueryable();

                if (isOutOfStock)
                {
                    query = query.Where(s => s.AvailableQuantity <= 0);
                }
                else if (isLowStock)
                {
                    query = query.Where(s => s.AvailableQuantity > 0 && s.AvailableQuantity <= 10);
                }

                return await query.OrderBy(s => s.AvailableQuantity).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stocks by status");
                throw;
            }
        }

        public async Task<Stock> CreateOrUpdateStockAsync(Stock stock)
        {
            try
            {
                var existingStock = await GetStockByProductVariantIdAsync(stock.ProductVariantId);
                
                if (existingStock != null)
                {
                    _logger.LogDebug("Updating existing stock for ProductVariant {ProductVariantId}. Available: {Available}, Reserved: {Reserved}", 
                        stock.ProductVariantId, stock.AvailableQuantity, stock.ReservedQuantity);

                    existingStock.AvailableQuantity = stock.AvailableQuantity;
                    existingStock.ReservedQuantity = stock.ReservedQuantity;
                    existingStock.LastUpdated = DateTime.UtcNow; // Ensure UTC for PostgreSQL
                    existingStock.LastUpdatedBy = stock.LastUpdatedBy ?? "System";
                    
                    _context.Stocks.Update(existingStock);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Stock updated successfully for ProductVariant {ProductVariantId}", stock.ProductVariantId);
                    return existingStock;
                }
                else
                {
                    _logger.LogDebug("Creating new stock for ProductVariant {ProductVariantId}. Available: {Available}, Reserved: {Reserved}", 
                        stock.ProductVariantId, stock.AvailableQuantity, stock.ReservedQuantity);

                    stock.LastUpdated = DateTime.UtcNow; // Ensure UTC for PostgreSQL
                    stock.LastUpdatedBy = stock.LastUpdatedBy ?? "System";

                    _context.Stocks.Add(stock);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Stock created successfully for ProductVariant {ProductVariantId}", stock.ProductVariantId);
                    return stock;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or updating stock for ProductVariant {ProductVariantId}. Error: {ErrorMessage}", 
                    stock.ProductVariantId, ex.Message);
                throw;
            }
        }

        public async Task<bool> UpdateStockQuantityAsync(int productVariantId, int availableQuantity, int reservedQuantity, string updatedBy)
        {
            try
            {
                var stock = await GetStockByProductVariantIdAsync(productVariantId);
                if (stock == null) return false;

                stock.AvailableQuantity = availableQuantity;
                stock.ReservedQuantity = reservedQuantity;
                stock.LastUpdated = DateTime.Now;
                stock.LastUpdatedBy = updatedBy;

                _context.Stocks.Update(stock);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock quantity for product variant: {ProductVariantId}", productVariantId);
                return false;
            }
        }

        public async Task<bool> DeleteStockAsync(int id)
        {
            try
            {
                var stock = await GetStockByIdAsync(id);
                if (stock == null) return false;

                _context.Stocks.Remove(stock);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting stock: {Id}", id);
                return false;
            }
        }

        // ===== STOCK ENTRIES (Nh?p hàng) =====
        public async Task<IEnumerable<StockEntry>> GetAllStockEntriesAsync()
        {
            try
            {
                return await _context.StockEntries
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .Include(se => se.Supplier)
                    .OrderByDescending(se => se.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all stock entries");
                throw;
            }
        }

        public async Task<IEnumerable<StockEntry>> GetStockEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.StockEntries
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .Include(se => se.Supplier)
                    .Where(se => se.EntryDate >= startDate && se.EntryDate <= endDate)
                    .OrderByDescending(se => se.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock entries by date range");
                throw;
            }
        }

        public async Task<IEnumerable<StockEntry>> GetStockEntriesBySupplierAsync(int supplierId)
        {
            try
            {
                return await _context.StockEntries
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .Include(se => se.Supplier)
                    .Where(se => se.SupplierId == supplierId)
                    .OrderByDescending(se => se.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock entries by supplier: {SupplierId}", supplierId);
                throw;
            }
        }

        public async Task<IEnumerable<StockEntry>> GetUnprocessedStockEntriesAsync()
        {
            try
            {
                return await _context.StockEntries
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .Include(se => se.Supplier)
                    .Where(se => !se.IsProcessed)
                    .OrderBy(se => se.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unprocessed stock entries");
                throw;
            }
        }

        public async Task<StockEntry?> GetStockEntryByIdAsync(int id)
        {
            try
            {
                return await _context.StockEntries
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .Include(se => se.Supplier)
                    .FirstOrDefaultAsync(se => se.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock entry by id: {Id}", id);
                throw;
            }
        }

        public async Task<StockEntry> CreateStockEntryAsync(StockEntry stockEntry)
        {
            try
            {
                _logger.LogDebug("=== CREATE STOCK ENTRY REPOSITORY STARTED ===");
                _logger.LogDebug("ProductVariantId: {ProductVariantId}, SupplierId: {SupplierId}, Quantity: {Quantity}, UnitCost: {UnitCost}", 
                    stockEntry.ProductVariantId, stockEntry.SupplierId, stockEntry.QuantityReceived, stockEntry.UnitCost);

                // Ensure UTC timestamp for PostgreSQL
                stockEntry.EntryDate = DateTime.UtcNow;
                stockEntry.IsProcessed = false;
                
                _logger.LogDebug("Adding stock entry to context...");
                _context.StockEntries.Add(stockEntry);

                _logger.LogDebug("Saving changes to database...");
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stock entry saved successfully to database with ID: {StockEntryId}", stockEntry.Id);

                return stockEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR in CreateStockEntryAsync repository. ProductVariantId: {ProductVariantId}, SupplierId: {SupplierId}. Exception: {ExceptionMessage}, InnerException: {InnerException}", 
                    stockEntry.ProductVariantId, stockEntry.SupplierId, ex.Message, ex.InnerException?.Message ?? "NONE");
                throw;
            }
        }

        public async Task<bool> UpdateStockEntryAsync(StockEntry stockEntry)
        {
            try
            {
                _context.StockEntries.Update(stockEntry);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock entry: {Id}", stockEntry.Id);
                return false;
            }
        }

        public async Task<bool> ProcessStockEntryAsync(int stockEntryId, string processedBy)
        {
            var entry = await GetStockEntryByIdAsync(stockEntryId);
            if (entry == null || entry.IsProcessed)
                return false;
            var stock = await GetStockByProductVariantIdAsync(entry.ProductVariantId);
            int beforeQty = stock?.AvailableQuantity ?? 0;
            if (stock == null)
            {
                stock = new Stock
                {
                    ProductVariantId = entry.ProductVariantId,
                    AvailableQuantity = entry.QuantityReceived,
                    ReservedQuantity = 0,
                    LastUpdated = DateTime.Now,
                    LastUpdatedBy = processedBy
                };
                await CreateOrUpdateStockAsync(stock);
            }
            else
            {
                stock.AvailableQuantity += entry.QuantityReceived;
                stock.LastUpdated = DateTime.Now;
                stock.LastUpdatedBy = processedBy;
                await CreateOrUpdateStockAsync(stock);
            }
            var transaction = new StockTransaction
            {
                ProductVariantId = entry.ProductVariantId,
                Type = StockTransactionType.StockIn,
                QuantityChange = entry.QuantityReceived,
                AvailableQuantityBefore = beforeQty,
                AvailableQuantityAfter = stock.AvailableQuantity,
                ReservedQuantityBefore = stock.ReservedQuantity,
                ReservedQuantityAfter = stock.ReservedQuantity,
                TransactionDate = DateTime.Now,
                Reason = "Nh?p kho t? phi?u nh?p",
                Notes = entry.Notes,
                CreatedBy = processedBy,
                ReferenceType = "StockEntry",
                ReferenceId = entry.Id
            };
            await CreateStockTransactionAsync(transaction);
            entry.IsProcessed = true;
            await UpdateStockEntryAsync(entry);
            return true;
        }

        public async Task<bool> CreateStockAuditAsync(int productVariantId, int systemQuantity, int actualQuantity, string auditedBy, string notes)
        {
            try
            {
                var stock = await GetStockByProductVariantIdAsync(productVariantId);
                if (stock == null) return false;

                var difference = actualQuantity - systemQuantity;
                // Create audit transaction
                var auditTransaction = new StockTransaction
                {
                    ProductVariantId = productVariantId,
                    Type = StockTransactionType.Adjustment,
                    QuantityChange = difference,
                    AvailableQuantityBefore = stock.AvailableQuantity,
                    AvailableQuantityAfter = actualQuantity,
                    ReservedQuantityBefore = stock.ReservedQuantity,
                    ReservedQuantityAfter = stock.ReservedQuantity,
                    Reason = $"Ki?m kho - Chênh l?ch: {difference}",
                    Notes = notes,
                    CreatedBy = auditedBy,
                    ReferenceType = "StockAudit",
                    TransactionDate = DateTime.Now
                };
                await CreateStockTransactionAsync(auditTransaction);
                // Update stock quantity
                stock.AvailableQuantity = actualQuantity;
                stock.LastUpdated = DateTime.Now;
                stock.LastUpdatedBy = auditedBy;
                _context.Stocks.Update(stock);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock audit for product variant: {ProductVariantId}", productVariantId);
                return false;
            }
        }

        // ===== STOCK TRANSACTIONS (L?ch s?) =====
        public async Task<IEnumerable<StockTransaction>> GetAllStockTransactionsAsync()
        {
            try
            {
                return await _context.StockTransactions
                    .Include(st => st.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .OrderByDescending(st => st.TransactionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all stock transactions");
                throw;
            }
        }

        public async Task<IEnumerable<StockTransaction>> GetStockTransactionsByProductVariantAsync(int productVariantId)
        {
            try
            {
                return await _context.StockTransactions
                    .Include(st => st.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Where(st => st.ProductVariantId == productVariantId)
                    .OrderByDescending(st => st.TransactionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock transactions by product variant: {ProductVariantId}", productVariantId);
                throw;
            }
        }

        public async Task<IEnumerable<StockTransaction>> GetStockTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.StockTransactions
                    .Include(st => st.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Where(st => st.TransactionDate >= startDate && st.TransactionDate <= endDate)
                    .OrderByDescending(st => st.TransactionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock transactions by date range");
                throw;
            }
        }

        public async Task<IEnumerable<StockTransaction>> GetStockTransactionsByTypeAsync(StockTransactionType type)
        {
            try
            {
                return await _context.StockTransactions
                    .Include(st => st.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Where(st => st.Type == type)
                    .OrderByDescending(st => st.TransactionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock transactions by type: {Type}", type);
                throw;
            }
        }

        public async Task<StockTransaction> CreateStockTransactionAsync(StockTransaction transaction)
        {
            try
            {
                // Ensure UTC timestamp for PostgreSQL
                transaction.TransactionDate = DateTime.UtcNow;
                
                _logger.LogDebug("Creating stock transaction for ProductVariant {ProductVariantId}. Type: {Type}, Quantity Change: {QuantityChange}", 
                    transaction.ProductVariantId, transaction.Type, transaction.QuantityChange);

                _context.StockTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stock transaction created successfully. ID: {TransactionId}, ProductVariant: {ProductVariantId}", 
                    transaction.Id, transaction.ProductVariantId);

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock transaction for ProductVariant {ProductVariantId}. Type: {Type}, Error: {ErrorMessage}", 
                    transaction.ProductVariantId, transaction.Type, ex.Message);
                throw;
            }
        }

        // ===== STOCK AUDIT (Ki?m kho) =====
        public async Task<IEnumerable<Stock>> GetStocksForAuditAsync()
        {
            try
            {
                return await _context.Stocks
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .OrderBy(s => s.ProductVariant.Product.Name)
                    .ThenBy(s => s.ProductVariant.Color)
                    .ThenBy(s => s.ProductVariant.Size)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stocks for audit");
                throw;
            }
        }

        public async Task<IEnumerable<StockTransaction>> GetAuditHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.StockTransactions
                    .Include(st => st.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Where(st => st.Type == StockTransactionType.Adjustment && st.ReferenceType == "StockAudit");
                if (startDate.HasValue)
                    query = query.Where(st => st.TransactionDate >= startDate);
                if (endDate.HasValue)
                    query = query.Where(st => st.TransactionDate <= endDate);
                return await query.OrderByDescending(st => st.TransactionDate).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit history");
                throw;
            }
        }

        public async Task<decimal> GetTotalStockValueAsync()
        {
            try
            {
                var totalValue = await (from s in _context.Stocks
                                      join pv in _context.ProductVariants on s.ProductVariantId equals pv.Id
                                      select s.AvailableQuantity * pv.Price).SumAsync();
                return totalValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total stock value");
                return 0;
            }
        }

        public async Task<int> GetTotalStockQuantityAsync()
        {
            try
            {
                return await _context.Stocks.SumAsync(s => s.AvailableQuantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total stock quantity");
                return 0;
            }
        }

        public async Task<int> GetLowStockCountAsync()
        {
            try
            {
                return await _context.Stocks.CountAsync(s => s.AvailableQuantity > 0 && s.AvailableQuantity <= 10);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock count");
                return 0;
            }
        }

        public async Task<int> GetOutOfStockCountAsync()
        {
            try
            {
                return await _context.Stocks.CountAsync(s => s.AvailableQuantity <= 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting out of stock count");
                return 0;
            }
        }

        public async Task<IEnumerable<Stock>> GetTopStockMovementsAsync(int count = 10)
        {
            try
            {
                var recentTransactionVariantIds = await _context.StockTransactions
                    .Where(st => st.TransactionDate >= DateTime.Now.AddDays(-30))
                    .GroupBy(st => st.ProductVariantId)
                    .OrderByDescending(g => g.Sum(st => Math.Abs(st.QuantityChange)))
                    .Select(g => g.Key)
                    .Take(count)
                    .ToListAsync();
                return await _context.Stocks
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Where(s => recentTransactionVariantIds.Contains(s.ProductVariantId))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top stock movements");
                return new List<Stock>();
            }
        }

        public async Task<Dictionary<string, int>> GetStockSummaryByStatusAsync()
        {
            try
            {
                var inStock = await _context.Stocks.CountAsync(s => s.AvailableQuantity > 10);
                var lowStock = await _context.Stocks.CountAsync(s => s.AvailableQuantity > 0 && s.AvailableQuantity <= 10);
                var outOfStock = await _context.Stocks.CountAsync(s => s.AvailableQuantity <= 0);
                return new Dictionary<string, int>
                {
                    ["InStock"] = inStock,
                    ["LowStock"] = lowStock,
                    ["OutOfStock"] = outOfStock,
                    ["Total"] = inStock + lowStock + outOfStock
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock summary by status");
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, decimal>> GetStockValueBySupplierAsync()
        {
            try
            {
                var result = await (from se in _context.StockEntries
                                  join s in _context.Suppliers on se.SupplierId equals s.Id
                                  where se.IsProcessed
                                  group se by s.Name into g
                                  select new { SupplierName = g.Key, TotalValue = g.Sum(se => se.TotalCost) })
                                  .ToDictionaryAsync(x => x.SupplierName, x => x.TotalValue);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock value by supplier");
                return new Dictionary<string, decimal>();
            }
        }

        public async Task<IEnumerable<Stock>> SearchStocksAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllStocksAsync();
                searchTerm = searchTerm.ToLower();
                return await _context.Stocks
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .Where(s => s.ProductVariant.Product.Name.ToLower().Contains(searchTerm) ||
                               s.ProductVariant.Color.ToLower().Contains(searchTerm) ||
                               s.ProductVariant.Size.ToLower().Contains(searchTerm) ||
                               s.ProductVariant.Product.Category.Name.ToLower().Contains(searchTerm) ||
                               s.ProductVariant.Product.Brand.Name.ToLower().Contains(searchTerm))
                    .OrderBy(s => s.ProductVariant.Product.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching stocks with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<StockEntry>> SearchStockEntriesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllStockEntriesAsync();
                searchTerm = searchTerm.ToLower();
                return await _context.StockEntries
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Include(se => se.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Brand)
                    .Include(se => se.Supplier)
                    .Where(se => se.ProductVariant.Product.Name.ToLower().Contains(searchTerm) ||
                                se.BatchNumber.ToLower().Contains(searchTerm) ||
                                se.Supplier.Name.ToLower().Contains(searchTerm))
                    .OrderByDescending(se => se.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching stock entries with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<Stock>> GetStocksByProductAsync(int productId)
        {
            try
            {
                return await _context.Stocks
                    .Include(s => s.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Where(s => s.ProductVariant.Product.Id == productId)
                    .OrderBy(s => s.ProductVariant.Color)
                    .ThenBy(s => s.ProductVariant.Size)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stocks by product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> DeleteStockEntryAsync(int id)
        {
            try
            {
                var entry = await GetStockEntryByIdAsync(id);
                if (entry == null)
                    return false;
                _context.StockEntries.Remove(entry);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting stock entry: {Id}", id);
                return false;
            }
        }
    }
}