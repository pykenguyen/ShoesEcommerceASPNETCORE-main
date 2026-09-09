using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Repositories.Interfaces;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;
using Microsoft.Extensions.Logging;

namespace ShoesEcommerce.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StaffRepository> _logger;

        public StaffRepository(AppDbContext context, ILogger<StaffRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Authentication Methods
        public async Task<Staff?> ValidateStaffAsync(string email, string password)
        {
            try
            {
                var staff = await _context.Staffs
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (staff != null && BCrypt.Net.BCrypt.Verify(password, staff.PasswordHash))
                {
                    _logger.LogInformation("Staff validation successful for {Email}", email);
                    return staff;
                }

                _logger.LogWarning("Staff validation failed for {Email}", email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating staff: {Email}", email);
                throw;
            }
        }

        // CRUD Operations
        public async Task<IEnumerable<Staff>> GetAllStaffsAsync()
        {
            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<Staff?> GetStaffByIdAsync(int id)
        {
            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Staff> CreateStaffAsync(Staff staff)
        {
            _context.Staffs.Add(staff);
            await _context.SaveChangesAsync();
            return staff;
        }

        public async Task<Staff> UpdateStaffAsync(Staff staff)
        {
            _context.Entry(staff).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return staff;
        }

        public async Task<bool> DeleteStaffAsync(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null)
                return false;

            // Remove associated UserRoles first
            var userRoles = await _context.UserRoles
                .Where(ur => ur.StaffId == id)
                .ToListAsync();
            _context.UserRoles.RemoveRange(userRoles);

            _context.Staffs.Remove(staff);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> StaffExistsAsync(int id)
        {
            return await _context.Staffs.AnyAsync(s => s.Id == id);
        }

        // Role Management
        public async Task<IEnumerable<Role>> GetStaffRolesAsync(int staffId)
        {
            return await _context.UserRoles
                .Where(ur => ur.StaffId == staffId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<bool> AssignRoleToStaffAsync(int staffId, int roleId)
        {
            // Check if staff exists
            if (!await StaffExistsAsync(staffId))
                return false;

            // Check if role exists and is for staff
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null || role.UserType != UserType.Staff)
                return false;

            // Check if assignment already exists
            var existingAssignment = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.StaffId == staffId && ur.RoleId == roleId);

            if (existingAssignment != null)
                return false; // Already assigned

            var userRole = new UserRole
            {
                StaffId = staffId,
                RoleId = roleId
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleFromStaffAsync(int staffId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.StaffId == staffId && ur.RoleId == roleId);

            if (userRole == null)
                return false;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Role>> GetAvailableStaffRolesAsync()
        {
            return await _context.Roles
                .Where(r => r.UserType == UserType.Staff)
                .ToListAsync();
        }

        // Department Management
        public async Task<IEnumerable<DepartmentEntity>> GetAllDepartmentsAsync()
        {
            return await _context.Departments
                .Include(d => d.Staffs)
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetStaffsByDepartmentAsync(int departmentId)
        {
            return await _context.Staffs
                .Where(s => s.DepartmentId == departmentId)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<DepartmentEntity?> GetDepartmentByIdAsync(int departmentId)
        {
            return await _context.Departments
                .Include(d => d.Staffs)
                .FirstOrDefaultAsync(d => d.Id == departmentId);
        }

        // Advanced Queries
        public async Task<IEnumerable<Staff>> SearchStaffsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllStaffsAsync();

            searchTerm = searchTerm.ToLower();

            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .Where(s => 
                    s.FirstName.ToLower().Contains(searchTerm) ||
                    s.LastName.ToLower().Contains(searchTerm) ||
                    s.Email.ToLower().Contains(searchTerm) ||
                    s.Department.Name.ToLower().Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetStaffsWithRolesAsync()
        {
            return await _context.Staffs
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .Where(s => s.Roles.Any())
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetStaffsWithDepartmentAsync()
        {
            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<int> GetTotalStaffCountAsync()
        {
            return await _context.Staffs.CountAsync();
        }

        public async Task<IEnumerable<Staff>> GetPaginatedStaffsAsync(int pageNumber, int pageSize)
        {
            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // ===== CLEAN ARCHITECTURE: Registration with Transaction =====
        // All data operations in Repository layer (following Repository Pattern)
        // No business logic in Service - Service only validates and orchestrates

        /// <summary>
        /// Complete staff registration with transaction support
        /// Implements Single Responsibility: Handles all data operations for staff registration
        /// Implements Atomicity: All operations succeed or all fail (transaction)
        /// </summary>
        public async Task<Staff?> RegisterStaffWithRoleAsync(Staff staff, string roleName)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("?? Starting staff registration for {Email} with role {RoleName}", 
                    staff.Email, roleName);

                // Step 1: Add Staff to DbContext
                _context.Staffs.Add(staff);

                // Step 2: Save to generate Staff ID
                await _context.SaveChangesAsync();

                _logger.LogInformation("? Staff created with ID: {StaffId}", staff.Id);

                // Step 3: Get or create the role
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == roleName && r.UserType == UserType.Staff);

                if (role == null)
                {
                    _logger.LogInformation("? Creating new staff role: {RoleName}", roleName);
                    role = new Role
                    {
                        Name = roleName,
                        UserType = UserType.Staff
                    };
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("? Role created with ID: {RoleId}", role.Id);
                }

                // Step 4: Assign role to staff
                var userRole = new UserRole
                {
                    StaffId = staff.Id,
                    RoleId = role.Id
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("?? Role {RoleName} (ID: {RoleId}) assigned to Staff {StaffId}", 
                    roleName, role.Id, staff.Id);

                // Step 5: Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation("?? Staff registration completed successfully for {Email}", staff.Email);

                return staff;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "? Error in staff registration for {Email}", staff.Email);
                throw; // Re-throw to let service layer handle
            }
        }

        // ===== Validation Methods (Data Layer) =====

        public async Task<Staff?> GetStaffByEmailAsync(string email)
        {
            return await _context.Staffs
                .Include(s => s.Department)
                .Include(s => s.Roles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Staffs
                .AnyAsync(s => s.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> PhoneExistsAsync(string phoneNumber)
        {
            return await _context.Staffs
                .AnyAsync(s => s.PhoneNumber == phoneNumber);
        }

        // ===== Password Management =====

        public async Task<bool> UpdatePasswordAsync(int staffId, string passwordHash)
        {
            var staff = await _context.Staffs.FindAsync(staffId);
            if (staff == null)
                return false;

            staff.PasswordHash = passwordHash;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}