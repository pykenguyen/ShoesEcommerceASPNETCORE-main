using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoesEcommerce.Services.Interfaces
{
    public interface IOrderService
    {
        Task<OrderViewModel> GetOrderByIdAsync(int orderId, int customerId);
        Task<List<OrderViewModel>> GetCustomerOrdersAsync(int customerId);
        Task<Order> CreateOrderAsync(CreateOrderViewModel model, int customerId);
        Task<CheckoutViewModel> GetCheckoutDataAsync(int customerId);
        Task<ShippingAddress> CreateShippingAddressAsync(CreateShippingAddressViewModel model, int customerId);
        Task<List<ShippingAddress>> GetCustomerShippingAddressesAsync(int customerId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<bool> UpdatePaymentStatusAsync(int orderId, string status);
        Task<string> GenerateOrderNumberAsync();
        Task<decimal> CalculateShippingFeeAsync(string city, string district);
        Task<List<OrderViewModel>> GetOrdersByStatusAsync(string status);
        Task<OrderViewModel?> GetOrderByOrderNumberAndPhoneAsync(string orderNumber, string phone);
    }
}
