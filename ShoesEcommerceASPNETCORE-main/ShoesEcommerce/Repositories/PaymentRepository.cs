using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Repositories.Interfaces;

namespace ShoesEcommerce.Repositories
{
    /// <summary>
    /// Repository for Payment entity operations
    /// </summary>
    public class PaymentRepository : IPaymentRepository
    {
        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }

        private readonly AppDbContext _context;
        private readonly ILogger<PaymentRepository> _logger;

        public PaymentRepository(AppDbContext context, ILogger<PaymentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Payment?> GetByOrderIdAsync(int orderId)
        {
            try
            {
                return await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.OrderId == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by id {Id}", id);
                throw;
            }
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            try
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for order {OrderId}", payment.OrderId);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Payment payment)
        {
            try
            {
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment {PaymentId}", payment.Id);
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(int orderId, string status, DateTime? paidAt = null, string? transactionId = null)
        {
            try
            {
                var payment = await GetByOrderIdAsync(orderId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for order {OrderId}", orderId);
                    return false;
                }

                payment.Status = status;
                
                if (paidAt.HasValue)
                {
                    payment.PaidAt = EnsureUtc(paidAt.Value);
                }

                if (!string.IsNullOrEmpty(transactionId))
                {
                    payment.TransactionId = transactionId;
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Payment updated for order {OrderId}: Status={Status}, PaidAt={PaidAt}, TransactionId={TransactionId}",
                    orderId, status, paidAt, transactionId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.ProductVariant)
                            .ThenInclude(pv => pv!.Product)
                    .Include(o => o.Payment)
                    .Include(o => o.ShippingAddress)
                    .Include(o => o.Invoice)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order with details {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order?> GetOrderWithItemsForPaymentAsync(int orderId)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.ProductVariant)
                            .ThenInclude(pv => pv!.Product)
                    .Include(o => o.Invoice)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order with items for payment {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> OrderExistsAsync(int orderId)
        {
            try
            {
                return await _context.Orders.AnyAsync(o => o.Id == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if order exists {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> OrderBelongsToCustomerAsync(int orderId, int customerId)
        {
            try
            {
                return await _context.Orders
                    .AnyAsync(o => o.Id == orderId && o.CustomerId == customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking order ownership {OrderId}, {CustomerId}", orderId, customerId);
                throw;
            }
        }

        public async Task<Invoice?> GetInvoiceByOrderIdAsync(int orderId)
        {
            try
            {
                return await _context.Invoices
                    .FirstOrDefaultAsync(i => i.OrderId == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Invoice> CreateOrUpdateInvoiceAsync(int orderId, string invoiceNumber, decimal amount)
        {
            try
            {
                var existingInvoice = await GetInvoiceByOrderIdAsync(orderId);
                
                if (existingInvoice != null)
                {
                    existingInvoice.InvoiceNumber = invoiceNumber;
                    existingInvoice.Amount = amount;
                    existingInvoice.IssuedAt = DateTime.UtcNow;
                    existingInvoice.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Invoice updated for order {OrderId}: {InvoiceNumber}", orderId, invoiceNumber);
                    return existingInvoice;
                }
                
                var invoice = new Invoice
                {
                    OrderId = orderId,
                    InvoiceNumber = invoiceNumber,
                    Amount = amount,
                    IssuedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    Status = InvoiceStatus.Draft,
                    Currency = "VND"
                };
                
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Invoice created for order {OrderId}: {InvoiceNumber}", orderId, invoiceNumber);
                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating invoice for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> UpdateInvoiceOnPaymentAsync(int orderId, string transactionId, DateTime paidAt)
        {
            try
            {
                var invoice = await GetInvoiceByOrderIdAsync(orderId);
                
                if (invoice == null)
                {
                    // Create invoice if it doesn't exist
                    var order = await _context.Orders.FindAsync(orderId);
                    if (order == null)
                    {
                        _logger.LogWarning("Order {OrderId} not found when updating invoice", orderId);
                        return false;
                    }
                    
                    // ✅ FIX: Use correct invoice number format: INV-{orderId}-{yyyyMMdd}
                    invoice = new Invoice
                    {
                        OrderId = orderId,
                        InvoiceNumber = $"INV-{orderId}-{paidAt:yyyyMMdd}",
                        Amount = order.TotalAmount,
                        IssuedAt = EnsureUtc(paidAt),
                        CreatedAt = DateTime.UtcNow,
                        Status = InvoiceStatus.Draft,
                        Currency = "VND"
                    };
                    
                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation(
                        "Invoice created on payment completion for order {OrderId}: {InvoiceNumber}, TransactionId: {TransactionId}",
                        orderId, invoice.InvoiceNumber, transactionId);
                }
                else
                {
                    // Update existing invoice
                    invoice.IssuedAt = EnsureUtc(paidAt);
                    invoice.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation(
                        "Invoice updated on payment completion for order {OrderId}: {InvoiceNumber}, TransactionId: {TransactionId}",
                        orderId, invoice.InvoiceNumber, transactionId);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice on payment for order {OrderId}", orderId);
                throw;
            }
        }

        // ✅ NEW METHODS FOR INVOICE TRANSACTION SAFETY

        public async Task<bool> UpdateInvoiceStatusAsync(int orderId, InvoiceStatus status)
        {
            try
            {
                var invoice = await GetInvoiceByOrderIdAsync(orderId);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice not found for order {OrderId} when updating status", orderId);
                    return false;
                }

                invoice.Status = status;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invoice status updated for order {OrderId}: {Status}", orderId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice status for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> SetInvoicePayPalOrderIdAsync(int orderId, string paypalOrderId)
        {
            try
            {
                var invoice = await GetInvoiceByOrderIdAsync(orderId);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice not found for order {OrderId} when setting PayPal Order ID", orderId);
                    return false;
                }

                invoice.PayPalOrderId = paypalOrderId;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("PayPal Order ID set for invoice, Order {OrderId}: {PayPalOrderId}", orderId, paypalOrderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting PayPal Order ID for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> CancelInvoiceAsync(int orderId, string reason)
        {
            try
            {
                var invoice = await GetInvoiceByOrderIdAsync(orderId);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice not found for order {OrderId} when cancelling", orderId);
                    return false;
                }

                invoice.Status = InvoiceStatus.Cancelled;
                invoice.CancelledAt = DateTime.UtcNow;
                invoice.CancellationReason = reason;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invoice cancelled for order {OrderId}: {Reason}", orderId, reason);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling invoice for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> FinalizeInvoiceAsync(int orderId, string transactionId, DateTime paidAt)
        {
            try
            {
                var invoice = await GetInvoiceByOrderIdAsync(orderId);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice not found for order {OrderId} when finalizing", orderId);
                    return false;
                }

                invoice.Status = InvoiceStatus.Paid;
                invoice.PayPalTransactionId = transactionId;
                invoice.PaidAt = EnsureUtc(paidAt);
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Invoice finalized for order {OrderId}: TransactionId={TransactionId}, PaidAt={PaidAt}",
                    orderId, transactionId, paidAt);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing invoice for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    return false;
                }

                order.Status = status;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order status updated: {OrderId} -> {Status}", orderId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Invoice?> GetInvoiceByPayPalOrderIdAsync(string paypalOrderId)
        {
            try
            {
                return await _context.Invoices
                    .Include(i => i.Order)
                    .FirstOrDefaultAsync(i => i.PayPalOrderId == paypalOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice by PayPal Order ID {PayPalOrderId}", paypalOrderId);
                throw;
            }
        }

        public async Task<bool> SetInvoiceVnPayTransactionIdAsync(int orderId, string vnpayTransactionId)
        {
            try
            {
                var invoice = await GetInvoiceByOrderIdAsync(orderId);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice not found for order {OrderId} when setting VNPay Transaction ID", orderId);
                    return false;
                }

                invoice.VnPayTransactionId = vnpayTransactionId;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("VNPay Transaction ID set for invoice, Order {OrderId}: {VnPayTransactionId}", orderId, vnpayTransactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting VNPay Transaction ID for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> FinalizeVnPayInvoiceAsync(int orderId, string transactionId, string? bankCode, DateTime paidAt)
        {
            try
            {
                var invoice = await GetInvoiceByOrderIdAsync(orderId);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice not found for order {OrderId} when finalizing VNPay payment", orderId);
                    return false;
                }

                invoice.Status = InvoiceStatus.Paid;
                invoice.VnPayTransactionId = transactionId;
                invoice.VnPayBankCode = bankCode;
                invoice.PaidAt = EnsureUtc(paidAt);
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "VNPay Invoice finalized for order {OrderId}: TransactionId={TransactionId}, BankCode={BankCode}, PaidAt={PaidAt}",
                    orderId, transactionId, bankCode, paidAt);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing VNPay invoice for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> FinalizeVnPayInvoiceFullAsync(
            int orderId, 
            string transactionId, 
            string? txnRef,
            string? bankCode, 
            string? bankTranNo,
            string? cardType,
            DateTime paidAt)
        {
            try
            {
                var invoice = await GetInvoiceByOrderIdAsync(orderId);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice not found for order {OrderId} when finalizing VNPay payment", orderId);
                    return false;
                }

                invoice.Status = InvoiceStatus.Paid;
                invoice.VnPayTransactionId = transactionId;
                invoice.VnPayTxnRef = txnRef;
                invoice.VnPayBankCode = bankCode;
                invoice.VnPayBankTranNo = bankTranNo;
                invoice.VnPayCardType = cardType;
                invoice.PaidAt = EnsureUtc(paidAt);
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "VNPay Invoice fully finalized for order {OrderId}: TransactionId={TransactionId}, TxnRef={TxnRef}, BankCode={BankCode}, BankTranNo={BankTranNo}, CardType={CardType}",
                    orderId, transactionId, txnRef, bankCode, bankTranNo, cardType);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing VNPay invoice for order {OrderId}", orderId);
                throw;
            }
        }
    }
}
