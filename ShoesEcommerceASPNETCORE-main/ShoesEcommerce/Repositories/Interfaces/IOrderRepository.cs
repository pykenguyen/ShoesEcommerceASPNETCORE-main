using ShoesEcommerce.Models.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllOrdersAsync();
    }
}
