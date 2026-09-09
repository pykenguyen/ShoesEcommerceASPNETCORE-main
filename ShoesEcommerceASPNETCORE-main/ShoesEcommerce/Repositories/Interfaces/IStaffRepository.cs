using ShoesEcommerce.Models.Accounts;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface IStaffRepository
    {
        // Authentication
        Task<Staff?> ValidateStaffAsync(string email, string password);
        
        // CRUD Operations
        Task<IEnumerable<Staff>> GetAllStaffsAsync();
        Task<Staff?> GetStaffByIdAsync(int id);
        Task<Staff?> GetStaffByEmailAsync(string email); // ? Added
        Task<Staff> CreateStaffAsync(Staff staff);
        Task<Staff> UpdateStaffAsync(Staff staff);
        Task<bool> DeleteStaffAsync(int id);
        Task<bool> StaffExistsAsync(int id);

        // ===== Registration with Transaction (CLEAN - Repository handles data logic) =====
        /// <summary>
        /// Complete staff registration with transaction support
        /// Creates staff account and assigns role in a single atomic operation
        /// Follows Repository Pattern: All data operations in repository layer
        /// </summary>
        /// <param name="staff">Staff entity to create</param>
        /// <param name="roleName">Role name to assign (Admin/Manager/Staff)</param>
        /// <returns>Created staff with ID, or null if failed</returns>
        Task<Staff?> RegisterStaffWithRoleAsync(Staff staff, string roleName);

        // ===== Validation Methods =====
        Task<bool> EmailExistsAsync(string email);
        Task<bool> PhoneExistsAsync(string phoneNumber);

        // Role Management
        Task<IEnumerable<Role>> GetStaffRolesAsync(int staffId);
        Task<bool> AssignRoleToStaffAsync(int staffId, int roleId);
        Task<bool> RemoveRoleFromStaffAsync(int staffId, int roleId);
        Task<IEnumerable<Role>> GetAvailableStaffRolesAsync();

        // Department Management
        Task<IEnumerable<DepartmentEntity>> GetAllDepartmentsAsync();
        Task<IEnumerable<Staff>> GetStaffsByDepartmentAsync(int departmentId);
        Task<DepartmentEntity?> GetDepartmentByIdAsync(int departmentId);

        // Advanced Queries
        Task<IEnumerable<Staff>> SearchStaffsAsync(string searchTerm);
        Task<IEnumerable<Staff>> GetStaffsWithRolesAsync();
        Task<IEnumerable<Staff>> GetStaffsWithDepartmentAsync();
        Task<int> GetTotalStaffCountAsync();
        Task<IEnumerable<Staff>> GetPaginatedStaffsAsync(int pageNumber, int pageSize);
        
        // Password Management
        Task<bool> UpdatePasswordAsync(int staffId, string passwordHash);
    }
}