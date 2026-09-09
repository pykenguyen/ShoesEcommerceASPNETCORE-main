using System.Collections.Generic;

namespace ShoesEcommerce.ViewModels.Admin
{
    public class AdminReportViewModel
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalProducts { get; set; }
        public int TotalStock { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public List<decimal> RevenueByMonth { get; set; } = new List<decimal>();
    }
}
