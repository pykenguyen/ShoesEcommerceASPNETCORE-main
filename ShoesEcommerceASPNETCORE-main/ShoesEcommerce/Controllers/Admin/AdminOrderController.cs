using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Models.ViewModels;

namespace ShoesEcommerce.Controllers.Admin
{
    [Authorize(Roles = "Admin,Staff")]
    [Route("Admin/Order")]
    public class AdminOrderController : Controller
    {
        private readonly IOrderService _orderService;
        public AdminOrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Quản lý đơn hàng - Admin";
            return View();
        }

        [HttpGet("Pending")]
        public async Task<IActionResult> Pending()
        {
            ViewData["Title"] = "Đơn hàng Chờ Phê duyệt";
            var orders = await _orderService.GetOrdersByStatusAsync("Pending");
            return View(orders);
        }

        [HttpGet("Confirmed")]
        public async Task<IActionResult> Confirmed()
        {
            ViewData["Title"] = "Đơn hàng Đã Xác nhận";
            var orders = await _orderService.GetOrdersByStatusAsync("Confirmed");
            return View(orders);
        }

        [HttpGet("Completed")]
        public async Task<IActionResult> Completed()
        {
            ViewData["Title"] = "Đơn hàng Thành công";
            var orders = await _orderService.GetOrdersByStatusAsync("Completed");
            return View(orders);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id, 0); // 0 for admin, ignore customerId
            if (order == null)
                return NotFound();
            return View(order);
        }

        [HttpPost("UpdateStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var result = await _orderService.UpdateOrderStatusAsync(orderId, status);
            if (result)
            {
                TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công.";
            }
            else
            {
                TempData["Error"] = "Cập nhật trạng thái đơn hàng thất bại.";
            }
            return RedirectToAction("Confirmed");
        }

        [HttpPost("ConfirmOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var result = await _orderService.UpdateOrderStatusAsync(id, "Confirmed");
            if (result)
            {
                TempData["Success"] = "Đơn hàng đã được xác nhận thành công.";
            }
            else
            {
                TempData["Error"] = "Xác nhận đơn hàng thất bại.";
            }
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("CompleteOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var result = await _orderService.UpdateOrderStatusAsync(id, "Completed");
            if (result)
            {
                TempData["Success"] = "Đơn hàng đã được hoàn thành và giao hàng thành công.";
            }
            else
            {
                TempData["Error"] = "Hoàn thành đơn hàng thất bại.";
            }
            return RedirectToAction("Details", new { id });
        }
    }
}