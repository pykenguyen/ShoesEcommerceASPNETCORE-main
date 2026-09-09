using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Account;

namespace ShoesEcommerce.Controllers.Admin
{
    /// <summary>
    /// Controller for Admin Staff Management
    /// Following SOLID principles:
    /// - Single Responsibility: Only handles HTTP requests/responses for staff management
    /// - Dependency Inversion: Depends on abstractions (IStaffService, IStaffRegistrationService)
    /// </summary>
    [Authorize(Roles = "Admin")] // ✅ Only Admin can manage staff
    public class AdminStaffController : Controller
    {
        private readonly IStaffService _staffService;
        private readonly IStaffRegistrationService _staffRegistrationService;
        private readonly ILogger<AdminStaffController> _logger;

        public AdminStaffController(
            IStaffService staffService,
            IStaffRegistrationService staffRegistrationService,
            ILogger<AdminStaffController> logger)
        {
            _staffService = staffService;
            _staffRegistrationService = staffRegistrationService;
            _logger = logger;
        }

        // GET: Admin/Staff
        public async Task<IActionResult> Index(
            string searchTerm = "",
            int? departmentId = null,
            int page = 1,
            int pageSize = 10)
        {
            ViewData["Title"] = "Quản lý Nhân viên - Admin";

            try
            {
                _logger.LogInformation("Loading staff index page - Search: {SearchTerm}, Dept: {DepartmentId}, Page: {Page}",
                    searchTerm, departmentId, page);

                // Get staff list with pagination and filters
                var model = await _staffService.GetStaffsAsync(searchTerm, departmentId, page, pageSize);

                // Load departments for filter dropdown
                var departments = await _staffService.GetAllDepartmentsAsync();
                ViewBag.Departments = new SelectList(departments, "Id", "Name", departmentId);

                _logger.LogInformation("Staff index loaded: {StaffCount} staff found", model?.TotalCount ?? 0);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff index page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách nhân viên.";

                // Return empty model with departments for filter
                try
                {
                    var departments = await _staffService.GetAllDepartmentsAsync();
                    ViewBag.Departments = new SelectList(departments, "Id", "Name");
                }
                catch
                {
                    ViewBag.Departments = new SelectList(new List<object>(), "Id", "Name");
                }

                return View(new ViewModels.Staff.StaffListViewModel
                {
                    Staffs = new List<ViewModels.Staff.StaffInfo>(),
                    SearchTerm = searchTerm,
                    DepartmentId = departmentId,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0
                });
            }
        }

        // GET: Admin/Staff/Details/5
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi tiết Nhân viên - Admin";
            return View();
        }

        // ===== STAFF REGISTRATION (NEW - Phase 2) =====

        /// <summary>
        /// GET: Admin/Staff/Create
        /// Display staff registration form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Tạo tài khoản Nhân viên";

            try
            {
                var model = new RegisterStaffViewModel();

                // Load departments for dropdown
                var departments = await _staffService.GetAllDepartmentsAsync();
                ViewBag.Departments = new SelectList(departments, "Id", "Name");

                // Available roles
                ViewBag.AvailableRoles = new SelectList(new[] { "Admin", "Manager", "Staff" });

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff creation form");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form tạo nhân viên.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Admin/Staff/Create
        /// Process staff registration
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterStaffViewModel model)
        {
            ViewData["Title"] = "Tạo tài khoản Nhân viên";

            try
            {
                _logger.LogInformation("🚀 Admin attempting to create staff account for {Email}", model.Email);

                // Validate model state
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("⚠️ Model validation failed for staff creation: {Email}", model.Email);
                    
                    // Reload dropdowns
                    await LoadDropdownsAsync();
                    return View(model);
                }

                // Call service to register staff
                var result = await _staffRegistrationService.RegisterStaffAsync(model);

                if (result.Success)
                {
                    _logger.LogInformation("✅ Staff account created successfully: {Email} by Admin {AdminEmail}", 
                        model.Email, User.Identity?.Name);

                    TempData["SuccessMessage"] = $"Tài khoản nhân viên '{model.FullName}' đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("⚠️ Staff registration failed: {Email} - {Error}", 
                        model.Email, result.ErrorMessage);

                    // Add validation errors to ModelState
                    if (result.HasValidationErrors)
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            ModelState.AddModelError(error.Key, error.Value);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, result.ErrorMessage);
                    }

                    // Reload dropdowns
                    await LoadDropdownsAsync();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating staff account for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra trong quá trình tạo tài khoản. Vui lòng thử lại.");
                
                // Reload dropdowns
                await LoadDropdownsAsync();
                return View(model);
            }
        }

        // ===== PRIVATE HELPER METHODS =====

        /// <summary>
        /// Load dropdown data for form (DRY principle)
        /// </summary>
        private async Task LoadDropdownsAsync()
        {
            try
            {
                var departments = await _staffService.GetAllDepartmentsAsync();
                ViewBag.Departments = new SelectList(departments, "Id", "Name");
                ViewBag.AvailableRoles = new SelectList(new[] { "Admin", "Manager", "Staff" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dropdowns");
                ViewBag.Departments = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.AvailableRoles = new SelectList(new[] { "Staff" });
            }
        }
    }
}