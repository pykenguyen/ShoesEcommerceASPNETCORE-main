using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Controllers
{
    [AllowAnonymous] // Allow anonymous access for testing
    public class TestDataController : Controller
    {
        private readonly AppDbContext _context;

        public TestDataController(AppDbContext context)
        {
            _context = context;
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

        [HttpGet]
        public async Task<IActionResult> ForceSeed()
        {
            try
            {
                Console.WriteLine("?? MANUAL: Force seeding triggered from TestDataController...");
                
                // Force seed the database
                await DataSeeder.SeedAllDataAsync(_context);
                
                // Get current counts
                var departments = await _context.Departments.CountAsync();
                var roles = await _context.Roles.CountAsync();
                var permissions = await _context.Permissions.CountAsync();
                var rolePermissions = await _context.RolePermissions.CountAsync();
                var staff = await _context.Staffs.CountAsync();
                var suppliers = await _context.Suppliers.CountAsync();
                
                var result = new
                {
                    success = true,
                    message = "Database seeded successfully",
                    counts = new
                    {
                        departments,
                        roles,
                        permissions,
                        rolePermissions,
                        staff,
                        suppliers
                    },
                    staffList = await _context.Staffs.Select(s => new
                    {
                        s.Id,
                        s.Email,
                        s.FirstName,
                        s.LastName,
                        s.DepartmentId
                    }).ToListAsync()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? MANUAL: Error in force seeding: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }

        // Clear staff and force fresh seeding
        [HttpGet]
        public async Task<IActionResult> ClearAndSeed()
        {
            try
            {
                Console.WriteLine("?? MANUAL: Clear and seed triggered...");
                
                // Clear existing data
                var existingRolePermissions = await _context.RolePermissions.ToListAsync();
                _context.RolePermissions.RemoveRange(existingRolePermissions);
                
                var existingUserRoles = await _context.UserRoles.ToListAsync();
                _context.UserRoles.RemoveRange(existingUserRoles);
                
                var existingStaff = await _context.Staffs.ToListAsync();
                _context.Staffs.RemoveRange(existingStaff);
                
                var existingRoles = await _context.Roles.ToListAsync();
                _context.Roles.RemoveRange(existingRoles);
                
                var existingPermissions = await _context.Permissions.ToListAsync();
                _context.Permissions.RemoveRange(existingPermissions);
                
                await _context.SaveChangesAsync();
                Console.WriteLine("?? MANUAL: Cleared existing data");
                
                // Force seed the database
                await DataSeeder.SeedAllDataAsync(_context);
                
                // Get current counts
                var departments = await _context.Departments.CountAsync();
                var roles = await _context.Roles.CountAsync();
                var permissions = await _context.Permissions.CountAsync();
                var rolePermissions = await _context.RolePermissions.CountAsync();
                var staff = await _context.Staffs.CountAsync();
                var suppliers = await _context.Suppliers.CountAsync();
                
                var result = new
                {
                    success = true,
                    message = "Database cleared and seeded successfully",
                    counts = new
                    {
                        departments,
                        roles,
                        permissions,
                        rolePermissions,
                        staff,
                        suppliers
                    },
                    staffList = await _context.Staffs.Select(s => new
                    {
                        s.Id,
                        s.Email,
                        s.FirstName,
                        s.LastName,
                        s.DepartmentId
                    }).ToListAsync(),
                    rolesList = await _context.Roles.Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.UserType
                    }).ToListAsync()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? MANUAL: Error in clear and seed: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }

        // Test staff authentication directly
        [HttpPost]
        public async Task<IActionResult> TestStaffAuth([FromBody] dynamic credentials)
        {
            try
            {
                string email = credentials.Email;
                string password = credentials.Password;
                
                Console.WriteLine($"?? MANUAL: Testing staff auth for {email}");
                
                // Check if staff exists in database
                var staff = await _context.Staffs
                    .Include(s => s.Department)
                    .Include(s => s.Roles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(s => s.Email == email);
                
                if (staff == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Staff not found in database",
                        staffExists = false,
                        allStaff = await _context.Staffs.Select(s => s.Email).ToListAsync()
                    });
                }
                
                // Test password
                bool passwordValid = BCrypt.Net.BCrypt.Verify(password, staff.PasswordHash);
                
                return Json(new
                {
                    success = passwordValid,
                    message = passwordValid ? "Authentication successful" : "Password mismatch",
                    staffExists = true,
                    staffInfo = new
                    {
                        staff.Id,
                        staff.Email,
                        staff.FirstName,
                        staff.LastName,
                        Department = staff.Department?.Name,
                        Roles = staff.Roles?.Select(ur => ur.Role?.Name).ToList()
                    },
                    passwordValid = passwordValid,
                    hashTest = new
                    {
                        providedPassword = password,
                        storedHash = staff.PasswordHash,
                        hashVerification = passwordValid
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}",
                    exception = new
                    {
                        ex.Message,
                        ex.StackTrace
                    }
                });
            }
        }

        // Test database connection
        [HttpGet]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var staffCount = await _context.Staffs.CountAsync();
                var departmentCount = await _context.Departments.CountAsync();
                
                return Json(new
                {
                    success = true,
                    canConnect = canConnect,
                    staffCount = staffCount,
                    departmentCount = departmentCount,
                    message = "Database connection successful"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Database connection failed: {ex.Message}"
                });
            }
        }

        // Test endpoint
        [HttpGet]
        public IActionResult Test()
        {
            return Json(new 
            { 
                success = true, 
                message = "TestDataController is working", 
                timestamp = DateTime.Now 
            });
        }

        // Quick SQL fix to create admin account directly
        [HttpGet]
        public async Task<IActionResult> CreateAdminDirectly()
        {
            try
            {
                Console.WriteLine("?? MANUAL: Creating admin account directly with SQL...");
                
                // Get Administration department (create if doesn't exist)
                var adminDept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == "Administration");
                if (adminDept == null)
                {
                    adminDept = new ShoesEcommerce.Models.Departments.Department { Name = "Administration" };
                    _context.Departments.Add(adminDept);
                    await _context.SaveChangesAsync();
                }
                
                // Get Admin role (create if doesn't exist)
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole == null)
                {
                    adminRole = new ShoesEcommerce.Models.Accounts.Role 
                    { 
                        Name = "Admin", 
                        UserType = ShoesEcommerce.Models.Accounts.UserType.Staff 
                    };
                    _context.Roles.Add(adminRole);
                    await _context.SaveChangesAsync();
                }
                
                // Delete existing admin if exists
                var existingAdmin = await _context.Staffs.FirstOrDefaultAsync(s => s.Email == "admin");
                if (existingAdmin != null)
                {
                    _context.Staffs.Remove(existingAdmin);
                    await _context.SaveChangesAsync();
                }
                
                // Create new admin account
                var adminAccount = new ShoesEcommerce.Models.Accounts.Staff
                {
                    Email = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                    FirstName = "Admin",
                    LastName = "User",
                    PhoneNumber = "0901000000",
                    DepartmentId = adminDept.Id
                };
                
                _context.Staffs.Add(adminAccount);
                await _context.SaveChangesAsync();
                Console.WriteLine($"?? MANUAL: Admin account created with ID: {adminAccount.Id}");
                
                // Assign admin role
                var userRole = new ShoesEcommerce.Models.Accounts.UserRole
                {
                    StaffId = adminAccount.Id,
                    RoleId = adminRole.Id
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
                
                var result = new
                {
                    success = true,
                    message = "Admin account created successfully",
                    adminAccount = new
                    {
                        adminAccount.Id,
                        adminAccount.Email,
                        adminAccount.FirstName,
                        adminAccount.LastName,
                        DepartmentName = adminDept.Name,
                        RoleName = adminRole.Name
                    },
                    totalStaff = await _context.Staffs.CountAsync()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? MANUAL: Error creating admin directly: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }

        // Quick login bypass for admin testing
        [HttpPost]
        public async Task<IActionResult> QuickAdminLogin()
        {
            try
            {
                // Check if admin exists
                var admin = await _context.Staffs
                    .Include(s => s.Department)
                    .Include(s => s.Roles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(s => s.Email == "admin");
                
                if (admin != null)
                {
                    // Create a simple session or redirect
                    return Json(new
                    {
                        success = true,
                        message = "Admin found - redirect to admin panel",
                        adminInfo = new
                        {
                            admin.Id,
                            admin.Email,
                            admin.FirstName,
                            admin.LastName,
                            Department = admin.Department?.Name,
                            Roles = admin.Roles?.Select(ur => ur.Role?.Name).ToList()
                        },
                        redirectUrl = "/Admin"
                    });
                }
                
                return Json(new
                {
                    success = false,
                    message = "Admin account not found"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }
    }
}