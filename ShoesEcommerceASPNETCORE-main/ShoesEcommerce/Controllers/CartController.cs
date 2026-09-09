using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Products;
using ShoesEcommerce.ViewModels.Cart;
using System.Linq;

namespace ShoesEcommerce.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(AppDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Loading cart for customer {CustomerId} or session {SessionId}", customerId, sessionId);

                Cart? cart;
                if (customerId != 0)
                {
                    // Nếu đã đăng nhập, tìm Cart thông qua Customer.CartId
                    // ✅ FIX: Remove invalid ternary operator in ThenInclude
                    var customer = await _context.Customers
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart.CartItems.Where(ci => !ci.IsDeleted))
                                .ThenInclude(ci => ci.ProductVariant)
                                    .ThenInclude(pv => pv.Product)
                                        .ThenInclude(p => p.Brand)
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart.CartItems.Where(ci => !ci.IsDeleted))
                                .ThenInclude(ci => ci.ProductVariant)
                                    .ThenInclude(pv => pv.CurrentStock)
                        .FirstOrDefaultAsync(c => c.Id == customerId);
                    
                    cart = customer?.Cart;
                    
                    // If customer doesn't have a cart yet (edge case), create one
                    if (cart == null && customer != null)
                    {
                        _logger.LogInformation("Creating cart for customer {CustomerId} who doesn't have one yet", customerId);
                        cart = new Cart
                        {
                            SessionId = Guid.NewGuid().ToString(),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CartItems = new List<CartItem>()
                        };
                        _context.Carts.Add(cart);
                        await _context.SaveChangesAsync();
                        
                        // Link cart to customer
                        customer.CartId = cart.Id;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Created and linked cart {CartId} to customer {CustomerId}", cart.Id, customerId);
                    }
                }
                else
                {
                    // Nếu chưa đăng nhập, tìm Cart theo SessionId
                    cart = await _context.Carts
                        .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                            .ThenInclude(ci => ci.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                                    .ThenInclude(p => p.Brand)
                        .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                            .ThenInclude(ci => ci.ProductVariant)
                                .ThenInclude(pv => pv.CurrentStock)
                        .FirstOrDefaultAsync(c => c.SessionId == sessionId);
                }

                // ✅ Filter only non-deleted cart items
                var activeCartItems = cart?.CartItems?.Where(ci => !ci.IsDeleted).ToList() ?? new List<CartItem>();
                
                _logger.LogInformation("Cart found: {CartId}, Total items in collection: {TotalItems}, Active items: {ActiveItems}", 
                    cart?.Id, cart?.CartItems?.Count ?? 0, activeCartItems.Count);
                
                var cartItemVMs = activeCartItems.Select(ci => new CartItemViewModel
                {
                    Id = ci.Id,
                    ProductName = ci.ProductVariant?.Product?.Name ?? "Không có tên",
                    ImageUrl = ci.ProductVariant?.ImageUrl ?? "/images/no-image.png",
                    Color = ci.ProductVariant?.Color ?? "Chưa có",
                    Size = ci.ProductVariant?.Size ?? "Chưa có",
                    Brand = ci.ProductVariant?.Product?.Brand?.Name ?? "Chưa có",
                    Price = ci.ProductVariant?.Price ?? 0,
                    Quantity = ci.Quantity
                }).ToList();

                _logger.LogInformation("Cart loaded successfully with {CartItemCount} active items for customer {CustomerId} or session {SessionId}", 
                    cartItemVMs.Count, customerId, sessionId);

                ViewData["Title"] = "Giỏ hàng";
                return View(cartItemVMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart for customer {CustomerId} or session {SessionId}", 
                    GetCurrentCustomerId(), HttpContext.Session.Id);
                TempData["Error"] = "Có lỗi xảy ra khi tải giỏ hàng. Vui lòng thử lại.";
                
                ViewData["Title"] = "Giỏ hàng";
                return View(new List<CartItemViewModel>());
            }
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productVariantId, int quantity = 1)
        {
            try
            {
                if (productVariantId == 0 || quantity < 1)
                {
                    _logger.LogWarning("Invalid product variant ID {ProductVariantId} or quantity {Quantity} provided to AddToCart", productVariantId, quantity);
                    return Json(new { success = false, message = "ID biến thể sản phẩm hoặc số lượng không hợp lệ." });
                }

                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Adding product variant {ProductVariantId} to cart for customer {CustomerId} or session {SessionId}", 
                    productVariantId, customerId, sessionId);

                var productVariant = await _context.ProductVariants
                    .Include(pv => pv.Product)
                    .Include(pv => pv.CurrentStock)
                    .FirstOrDefaultAsync(pv => pv.Id == productVariantId);

                if (productVariant == null)
                {
                    _logger.LogWarning("Product variant {ProductVariantId} not found for AddToCart", productVariantId);
                    return Json(new { success = false, message = "Biến thể sản phẩm không tồn tại." });
                }

                if (productVariant.AvailableQuantity < quantity)
                {
                    _logger.LogWarning("Requested quantity {Quantity} exceeds available stock {AvailableQuantity} for product variant {ProductVariantId}", quantity, productVariant.AvailableQuantity, productVariantId);
                    return Json(new { success = false, message = $"Chỉ còn {productVariant.AvailableQuantity} sản phẩm trong kho." });
                }

                Cart? cart;
                if (customerId != 0)
                {
                    // Nếu đã đăng nhập, tìm Cart thông qua Customer.CartId
                    var customer = await _context.Customers
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart.CartItems)
                        .FirstOrDefaultAsync(c => c.Id == customerId);
                    
                    cart = customer?.Cart;
                }
                else
                {
                    // Nếu chưa đăng nhập, tìm Cart theo SessionId
                    cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.SessionId == sessionId);
                }

                if (cart == null)
                {
                    cart = new Cart
                    {
                        SessionId = sessionId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                    if (customerId != 0)
                    {
                        var customer = await _context.Customers.FindAsync(customerId);
                        if (customer != null)
                        {
                            customer.CartId = cart.Id;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                // ✅ Only look for non-deleted cart items when adding
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductVarientId == productVariantId && !ci.IsDeleted);

                if (cartItem != null)
                {
                    if (cartItem.Quantity + quantity > productVariant.AvailableQuantity)
                    {
                        return Json(new { success = false, message = $"Chỉ còn {productVariant.AvailableQuantity - cartItem.Quantity} sản phẩm có thể thêm." });
                    }
                    cartItem.Quantity += quantity;
                    cartItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductVarientId = productVariantId,
                        Quantity = quantity,
                        CreatedAt = DateTime.UtcNow,
                        PriceAtAddTime = productVariant.Price // ✅ Track price when added to cart
                    };
                    _context.CartItems.Add(cartItem);
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product variant {ProductVariantId} to cart for customer {CustomerId}", productVariantId, GetCurrentCustomerId());
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng. Vui lòng thử lại." });
            }
        }

        private int GetCurrentCustomerId()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return 0;

                if (int.TryParse(userIdClaim, out int customerId))
                    return customerId;

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current customer ID");
                return 0;
            }
        }

        // GET: Cart/RemoveFromCart - ✅ Soft delete for AI analytics
        [HttpGet]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            try
            {
                _logger.LogInformation("Removing cart item {CartItemId}", id);

                var cartItem = await _context.CartItems.FindAsync(id);
                if (cartItem != null && !cartItem.IsDeleted)
                {
                    // ✅ Soft delete instead of hard delete for AI analytics
                    cartItem.IsDeleted = true;
                    cartItem.DeletedAt = DateTime.UtcNow;
                    cartItem.DeletionReason = "Removed";
                    cartItem.UpdatedAt = DateTime.UtcNow;
                    
                    var cart = await _context.Carts.FindAsync(cartItem.CartId);
                    if (cart != null)
                    {
                        cart.UpdatedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cart item {CartItemId} soft-deleted successfully (Reason: Removed)", id);
                    TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
                }
                else
                {
                    _logger.LogWarning("Cart item {CartItemId} not found or already deleted", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi xóa sản phẩm khỏi giỏ hàng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            try
            {
                if (quantity < 1)
                {
                    _logger.LogWarning("Invalid quantity {Quantity} provided for cart item {CartItemId}", quantity, id);
                    TempData["Error"] = "Số lượng phải lớn hơn 0.";
                    return BadRequest("Số lượng phải lớn hơn 0.");
                }

                _logger.LogInformation("Updating quantity for cart item {CartItemId} to {Quantity}", id, quantity);

                var cartItem = await _context.CartItems
                    .Include(ci => ci.ProductVariant)
                        .ThenInclude(pv => pv.CurrentStock)
                    .FirstOrDefaultAsync(ci => ci.Id == id && !ci.IsDeleted); // ✅ Only update non-deleted items

                if (cartItem != null)
                {
                    // Check if requested quantity exceeds stock using the correct property
                    if (cartItem.ProductVariant != null && quantity > cartItem.ProductVariant.AvailableQuantity)
                    {
                        _logger.LogWarning("Requested quantity {Quantity} exceeds stock {AvailableQuantity} for cart item {CartItemId}", 
                            quantity, cartItem.ProductVariant.AvailableQuantity, id);
                        TempData["Error"] = $"Số lượng yêu cầu vượt quá hàng tồn kho. Chỉ còn {cartItem.ProductVariant.AvailableQuantity} sản phẩm.";
                        return Json(new { success = false, message = $"Chỉ còn {cartItem.ProductVariant.AvailableQuantity} sản phẩm trong kho." });
                    }

                    cartItem.Quantity = quantity;
                    cartItem.UpdatedAt = DateTime.UtcNow; // ✅ Track update time
                    
                    var cart = await _context.Carts.FindAsync(cartItem.CartId);
                    if (cart != null)
                    {
                        cart.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Cart item {CartItemId} quantity updated to {Quantity}", id, quantity);
                    TempData["Success"] = "Đã cập nhật số lượng sản phẩm.";
                }
                else
                {
                    _logger.LogWarning("Cart item {CartItemId} not found or already deleted for quantity update", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for cart item {CartItemId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật số lượng sản phẩm. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Cart/ClearCart - Clear entire cart (soft delete for AI analytics)
        [HttpGet]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Clearing cart for customer {CustomerId} or session {SessionId}", customerId, sessionId);

                Cart? cart;
                if (customerId != 0)
                {
                    var customer = await _context.Customers
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart.CartItems.Where(ci => !ci.IsDeleted))
                        .FirstOrDefaultAsync(c => c.Id == customerId);
                    
                    cart = customer?.Cart;
                }
                else
                {
                    cart = await _context.Carts
                        .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                        .FirstOrDefaultAsync(c => c.SessionId == sessionId);
                }

                var activeItems = cart?.CartItems?.Where(ci => !ci.IsDeleted).ToList();
                if (activeItems != null && activeItems.Any())
                {
                    // ✅ Soft delete all items instead of hard delete
                    var now = DateTime.UtcNow;
                    foreach (var item in activeItems)
                    {
                        item.IsDeleted = true;
                        item.DeletedAt = now;
                        item.DeletionReason = "CartCleared";
                        item.UpdatedAt = now;
                    }
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cart soft-deleted {ItemCount} items for customer {CustomerId} or session {SessionId}", 
                        activeItems.Count, customerId, sessionId);
                    TempData["Success"] = "Đã xóa tất cả sản phẩm khỏi giỏ hàng.";
                }
                else
                {
                    _logger.LogInformation("No items to clear in cart for customer {CustomerId} or session {SessionId}", customerId, sessionId);
                    TempData["Info"] = "Giỏ hàng đã trống.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for customer {CustomerId}", GetCurrentCustomerId());
                TempData["Error"] = "Có lỗi xảy ra khi xóa giỏ hàng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        // API: Cart/GetCartCount - Get cart item count for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                Cart? cart;
                if (customerId != 0)
                {
                    var customer = await _context.Customers
                        .Include(c => c.Cart)
                            .ThenInclude(cart => cart.CartItems.Where(ci => !ci.IsDeleted))
                        .FirstOrDefaultAsync(c => c.Id == customerId);
                    
                    cart = customer?.Cart;
                }
                else
                {
                    cart = await _context.Carts
                        .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                        .FirstOrDefaultAsync(c => c.SessionId == sessionId);
                }

                // ✅ Only count non-deleted items
                var count = cart?.CartItems?.Where(ci => !ci.IsDeleted).Sum(ci => ci.Quantity) ?? 0;
                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { count = 0 });
            }
        }
    }
}