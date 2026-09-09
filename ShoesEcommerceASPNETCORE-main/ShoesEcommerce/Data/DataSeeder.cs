using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Products;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;

namespace ShoesEcommerce.Data
{
    /// <summary>
    /// Data seeder for initial database population
    /// Includes RBAC (Role-Based Access Control) seeding
    /// </summary>
    public static class DataSeeder
    {
        /// <summary>
        /// Seed departments for staff organization
        /// </summary>
        public static async Task SeedDepartmentsAsync(AppDbContext context)
        {
            if (await context.Departments.AnyAsync())
            {
                Console.WriteLine("? Departments already seeded.");
                return;
            }

            Console.WriteLine("?? Seeding Departments...");

            var departments = new List<DepartmentEntity>
            {
                new DepartmentEntity { Name = "Administration" },
                new DepartmentEntity { Name = "Sales" },
                new DepartmentEntity { Name = "Marketing" },
                new DepartmentEntity { Name = "Customer Service" },
                new DepartmentEntity { Name = "Warehouse" },
                new DepartmentEntity { Name = "IT" }
            };

            await context.Departments.AddRangeAsync(departments);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"? Seeded {departments.Count} departments.");
        }

        /// <summary>
        /// Seed roles for RBAC (Customer and Staff roles)
        /// </summary>
        public static async Task SeedRolesAsync(AppDbContext context)
        {
            if (await context.Roles.AnyAsync())
            {
                Console.WriteLine("? Roles already seeded.");
                return;
            }

            Console.WriteLine("?? Seeding Roles...");

            var roles = new List<Role>
            {
                // Staff Roles
                new Role
                {
                    Name = "Admin",
                    UserType = UserType.Staff
                },
                new Role
                {
                    Name = "Manager",
                    UserType = UserType.Staff
                },
                new Role
                {
                    Name = "Staff",
                    UserType = UserType.Staff
                },
                
                // Customer Role
                new Role
                {
                    Name = "Customer",
                    UserType = UserType.Customer
                }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"? Seeded {roles.Count} roles.");
        }

        /// <summary>
        /// Seed first admin account + initial staff accounts
        /// IMPORTANT: First admin must be created for system bootstrap
        /// </summary>
        public static async Task SeedStaffAsync(AppDbContext context)
        {
            if (await context.Staffs.AnyAsync())
            {
                Console.WriteLine("? Staff already seeded.");
                return;
            }

            Console.WriteLine("?? Seeding Staff (including first admin)...");

            // Get required departments
            var adminDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Administration");
            var salesDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Sales");
            var itDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "IT");
            
            // Get roles
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" && r.UserType == UserType.Staff);
            var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Manager" && r.UserType == UserType.Staff);
            var staffRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Staff" && r.UserType == UserType.Staff);

            if (adminDept == null || salesDept == null || adminRole == null || staffRole == null)
            {
                Console.WriteLine("? Required departments or roles not found. Skipping staff seeding.");
                return;
            }

            // ?? IMPORTANT: First Admin Account (Bootstrap)
            var staffMembers = new List<Staff>
            {
                new Staff
                {
                    Email = "admin@shoesstore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FirstName = "Super",
                    LastName = "Admin",
                    PhoneNumber = "0901000001",
                    DepartmentId = adminDept.Id
                },
                new Staff
                {
                    Email = "manager@shoesstore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                    FirstName = "Department",
                    LastName = "Manager",
                    PhoneNumber = "0901000002",
                    DepartmentId = salesDept.Id
                },
                new Staff
                {
                    Email = "staff@shoesstore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                    FirstName = "Sales",
                    LastName = "Staff",
                    PhoneNumber = "0901000003",
                    DepartmentId = salesDept.Id
                }
            };

            // Add IT staff if IT department exists
            if (itDept != null)
            {
                staffMembers.Add(new Staff
                {
                    Email = "it@shoesstore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("IT@123"),
                    FirstName = "IT",
                    LastName = "Support",
                    PhoneNumber = "0901000004",
                    DepartmentId = itDept.Id
                });
            }

            await context.Staffs.AddRangeAsync(staffMembers);
            await context.SaveChangesAsync();

            Console.WriteLine($"? Seeded {staffMembers.Count} staff members.");

            // Assign roles to staff
            var admin = await context.Staffs.FirstOrDefaultAsync(s => s.Email == "admin@shoesstore.com");
            var manager = await context.Staffs.FirstOrDefaultAsync(s => s.Email == "manager@shoesstore.com");
            var staff = await context.Staffs.FirstOrDefaultAsync(s => s.Email == "staff@shoesstore.com");
            var itStaff = await context.Staffs.FirstOrDefaultAsync(s => s.Email == "it@shoesstore.com");

            if (admin != null && manager != null && staff != null)
            {
                var userRoles = new List<UserRole>
                {
                    new UserRole { StaffId = admin.Id, RoleId = adminRole.Id },
                    new UserRole { StaffId = manager.Id, RoleId = managerRole?.Id ?? staffRole.Id },
                    new UserRole { StaffId = staff.Id, RoleId = staffRole.Id }
                };

                // Assign IT staff role if exists
                if (itStaff != null)
                {
                    userRoles.Add(new UserRole { StaffId = itStaff.Id, RoleId = staffRole.Id });
                }

                await context.UserRoles.AddRangeAsync(userRoles);
                await context.SaveChangesAsync();
                
                Console.WriteLine($"? Assigned roles to {userRoles.Count} staff members.");
                Console.WriteLine("\n?? LOGIN CREDENTIALS:");
                Console.WriteLine("????????????????????????????????????????");
                Console.WriteLine("?? ADMIN:    admin@shoesstore.com    | Admin@123");
                Console.WriteLine("?? MANAGER:  manager@shoesstore.com  | Manager@123");
                Console.WriteLine("?? STAFF:    staff@shoesstore.com    | Staff@123");
                if (itStaff != null)
                {
                    Console.WriteLine("?? IT:       it@shoesstore.com       | IT@123");
                }
                Console.WriteLine("????????????????????????????????????????\n");
            }
        }

        /// <summary>
        /// Seed suppliers for product sourcing
        /// </summary>
        public static async Task SeedSuppliersAsync(AppDbContext context)
        {
            if (await context.Suppliers.AnyAsync())
            {
                Console.WriteLine("? Suppliers already seeded.");
                return;
            }

            Console.WriteLine("?? Seeding Suppliers...");

            var suppliers = new List<Supplier>
            {
                new Supplier
                {
                    Name = "Nike Vietnam",
                    ContactInfo = "nike@vietnam.com - 0901234567"
                },
                new Supplier
                {
                    Name = "Adidas Distribution",
                    ContactInfo = "adidas@supplier.com - 0902345678"
                },
                new Supplier
                {
                    Name = "Converse Official",
                    ContactInfo = "converse@official.vn - 0903456789"
                },
                new Supplier
                {
                    Name = "Vans Authentic",
                    ContactInfo = "vans@authentic.com - 0904567890"
                },
                new Supplier
                {
                    Name = "Local Shoe Manufacturer",
                    ContactInfo = "local@shoes.vn - 0905678901"
                }
            };

            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"? Seeded {suppliers.Count} suppliers.");
        }

        /// <summary>
        /// Seed all data in correct order (respecting foreign key constraints)
        /// </summary>
        public static async Task SeedAllDataAsync(AppDbContext context)
        {
            Console.WriteLine("\n?? ========================================");
            Console.WriteLine("?? STARTING DATABASE SEEDING");
            Console.WriteLine("?? ========================================\n");

            try
            {
                // Order matters! (Foreign key dependencies)
                await SeedRolesAsync(context);          // 1. Roles first (no dependencies)
                await SeedDepartmentsAsync(context);     // 2. Departments (no dependencies)
                await SeedStaffAsync(context);           // 3. Staff (depends on Roles & Departments)
                await SeedSuppliersAsync(context);       // 4. Suppliers (no dependencies)

                Console.WriteLine("\n? ========================================");
                Console.WriteLine("? DATABASE SEEDING COMPLETED SUCCESSFULLY");
                Console.WriteLine("? ========================================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n? ========================================");
                Console.WriteLine($"? ERROR DURING SEEDING: {ex.Message}");
                Console.WriteLine("? ========================================\n");
                throw;
            }
        }
    }
}