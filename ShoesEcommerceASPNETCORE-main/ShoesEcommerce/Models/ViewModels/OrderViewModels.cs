using System.ComponentModel.DataAnnotations;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.ViewModels
{
    // ViewModel cho việc tạo đơn hàng
    public class CreateOrderViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn địa chỉ giao hàng")]
        public int ShippingAddressId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; }

        public string? Note { get; set; }
    }

    // ViewModel cho việc hiển thị đơn hàng
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
        public ShippingAddressViewModel ShippingAddress { get; set; }
        public PaymentViewModel Payment { get; set; }
        public string CustomerName { get; set; } // Thêm thuộc tính CustomerName để hiển thị tên khách hàng
        public int CustomerId { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    // ViewModel cho chi tiết đơn hàng
    public class OrderDetailViewModel
    {
        public int Id { get; set; }
        public ProductVariantViewModel ProductVariant { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    // ViewModel cho địa chỉ giao hàng
    public class ShippingAddressViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
    }

    // ViewModel cho thanh toán
    public class PaymentViewModel
    {
        public int Id { get; set; }
        public string Method { get; set; }
        public string Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? TransactionId { get; set; }
        
        // ✅ NEW: Invoice info
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceIssuedAt { get; set; }
        
        // ✅ NEW: VNPay specific fields
        public string? VnPayTxnRef { get; set; }
        public string? VnPayBankCode { get; set; }
        public string? VnPayBankTranNo { get; set; }
        public string? VnPayCardType { get; set; }
        
        // ✅ NEW: PayPal specific fields
        public string? PayPalOrderId { get; set; }
        public string? PayPalTransactionId { get; set; }
        public decimal? AmountInUSD { get; set; }
        public decimal? ExchangeRate { get; set; }
    }

    // ViewModel cho sản phẩm variant
    public class ProductVariantViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public decimal Price { get; set; }
    }

    // ViewModel cho checkout
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }
        public List<ShippingAddressViewModel> AvailableAddresses { get; set; }
        public CreateOrderViewModel OrderInfo { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // ViewModel cho cart item
    public class CartItemViewModel
    {
        public int Id { get; set; }
        public ProductVariantViewModel ProductVariant { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    // ViewModel cho việc tạo địa chỉ giao hàng mới
    public class CreateShippingAddressViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [MaxLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Số điện thoại phải có 10-15 chữ số và có thể bắt đầu bằng +")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [MaxLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thành phố")]
        [MaxLength(50, ErrorMessage = "Tên thành phố không được quá 50 ký tự")]
        public string City { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn quận/huyện")]
        [MaxLength(50, ErrorMessage = "Tên quận/huyện không được quá 50 ký tự")]
        public string District { get; set; }
    }
}
