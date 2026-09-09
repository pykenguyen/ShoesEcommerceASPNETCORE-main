using ShoesEcommerce.ViewModels.Payment;

namespace ShoesEcommerce.Services.Payment
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(int orderId, decimal amount, HttpContext context);
        VnPayReturnModel ProcessReturn(IQueryCollection queryParams);
    }
}
