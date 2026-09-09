using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.ViewModels.Admin;
using ShoesEcommerce.Repositories.Interfaces;
using System.Linq;

namespace ShoesEcommerce.Controllers.Admin
{
    [Authorize(Roles = "Admin,Staff")] // Require Admin or Staff role
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IOrderRepository _orderRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;

        public AdminController(AppDbContext context, ILogger<AdminController> logger, IOrderRepository orderRepository, IStockRepository stockRepository, IProductRepository productRepository)
            : base()
        {
            _context = context;
            _logger = logger;
            _orderRepository = orderRepository;
            _stockRepository = stockRepository;
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Trang chủ Admin";
            
            var model = new AdminReportViewModel();
            var orders = await _orderRepository.GetAllOrdersAsync();
            model.TotalOrders = orders.Count();
            model.TotalRevenue = orders.Sum(o => o.TotalAmount);
            var products = await _productRepository.GetAllProductVariantsAsync();
            model.TotalProducts = products.Count();
            model.TotalStock = await _stockRepository.GetTotalStockQuantityAsync();
            model.LowStockCount = await _stockRepository.GetLowStockCountAsync();
            model.OutOfStockCount = await _stockRepository.GetOutOfStockCountAsync();
            // Calculate total cost from StockEntries (imported cost)
            model.TotalCost = products.SelectMany(p => p.StockEntries).Sum(se => se.UnitCost * se.QuantityReceived);
            // Calculate total stock quantity from CurrentStock
            model.TotalStock = products.Sum(p => p.AvailableQuantity);
            model.TotalProfit = model.TotalRevenue - model.TotalCost;
            var currentYear = DateTime.Now.Year;
            for (int month = 1; month <= 12; month++)
            {
                var monthRevenue = orders.Where(o => o.CreatedAt.Year == currentYear && o.CreatedAt.Month == month).Sum(o => o.TotalAmount);
                model.RevenueByMonth.Add(monthRevenue);
            }
            
            try
            {
                // Test database connection
                await _context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection successful");
                
                // Get basic counts
                var staffCount = await _context.Staffs.CountAsync();
                var departmentCount = await _context.Departments.CountAsync();
                var roleCount = await _context.Roles.CountAsync();
                
                ViewBag.StaffCount = staffCount;
                ViewBag.DepartmentCount = departmentCount;
                ViewBag.RoleCount = roleCount;
                ViewBag.DatabaseStatus = "Connected";
                
                _logger.LogInformation("Dashboard loaded successfully with {StaffCount} staff, {DepartmentCount} departments, {RoleCount} roles", 
                    staffCount, departmentCount, roleCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                ViewBag.DatabaseStatus = "Error: " + ex.Message;
                ViewBag.StaffCount = 0;
                ViewBag.DepartmentCount = 0;
                ViewBag.RoleCount = 0;
            }
            
            return View(model);
        }

        // Simple redirect to AdminStaffController
        public IActionResult Staff()
        {
            return RedirectToAction("Index", "AdminStaff");
        }

        public IActionResult Customer()
        {
            ViewData["Title"] = "Khách hàng";
            return View();
        }

        public IActionResult Pending()
        {
            ViewData["Title"] = "Đơn hàng Chờ Phê duyệt";
            return View();
        }

        public IActionResult Confirmed()
        {
            ViewData["Title"] = "Đơn hàng Đã Xác nhận";
            return View();
        }

        public IActionResult Completed()
        {
            ViewData["Title"] = "Đơn hàng Thành công";
            return View();
        }

        public IActionResult Inventory()
        {
            ViewData["Title"] = "Quản lý Tồn kho";
            return View();
        }

        public IActionResult Check()
        {
            ViewData["Title"] = "Kiểm kho";
            return View();
        }

        public IActionResult Import()
        {
            ViewData["Title"] = "Nhập hàng";
            return View();
        }

        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Bảng điều khiển Admin";
            return View();
        }

        // Health check endpoint
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var staffCount = await _context.Staffs.CountAsync();
                var departmentCount = await _context.Departments.CountAsync();
                var roleCount = await _context.Roles.CountAsync();

                return Json(new
                {
                    status = "healthy",
                    database = canConnect ? "connected" : "disconnected",
                    staffCount,
                    departmentCount,
                    roleCount,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return Json(new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }
    }
}
