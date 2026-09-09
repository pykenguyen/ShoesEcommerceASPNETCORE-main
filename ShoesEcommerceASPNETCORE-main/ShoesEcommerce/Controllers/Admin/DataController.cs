using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Controllers.Admin
{
    public class DataController : Controller
    {
        private readonly AppDbContext _context;

        public DataController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SeedSuppliers()
        {
            try
            {
                // Check if suppliers already exist
                if (await _context.Suppliers.AnyAsync())
                {
                    var existingCount = await _context.Suppliers.CountAsync();
                    return Json(new { success = true, message = $"Suppliers already exist. Count: {existingCount}" });
                }

                var suppliers = new List<Supplier>
                {
                    new Supplier { Name = "Nike Vietnam", ContactInfo = "nike@vietnam.com - 0901234567" },
                    new Supplier { Name = "Adidas Distribution", ContactInfo = "adidas@supplier.com - 0902345678" },
                    new Supplier { Name = "Converse Official", ContactInfo = "converse@official.vn - 0903456789" },
                    new Supplier { Name = "Vans Authentic", ContactInfo = "vans@authentic.com - 0904567890" },
                    new Supplier { Name = "Local Shoe Manufacturer", ContactInfo = "local@shoes.vn - 0905678901" }
                };

                await _context.Suppliers.AddRangeAsync(suppliers);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Successfully created {suppliers.Count} suppliers" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckSuppliers()
        {
            try
            {
                var suppliers = await _context.Suppliers.ToListAsync();
                return Json(new 
                { 
                    success = true, 
                    count = suppliers.Count,
                    suppliers = suppliers.Select(s => new { s.Id, s.Name, s.ContactInfo })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}