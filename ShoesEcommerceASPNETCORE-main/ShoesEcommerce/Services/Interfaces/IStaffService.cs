using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.ViewModels.Account;
using ShoesEcommerce.ViewModels.Staff;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;

namespace ShoesEcommerce.Services.Interfaces
{
    public interface IStaffService
    {
        // Authentication
        Task<StaffLoginResult> LoginStaffAsync(StaffLoginViewModel model);
        Task<bool> ValidateStaffAsync(string email, string password);
        
        // Staff Management
        Task<StaffListViewModel> GetStaffsAsync(string searchTerm, int? departmentId, int page, int pageSize);
        Task<Staff?> GetStaffByIdAsync(int id);
        Task<StaffInfo> CreateStaffAsync(CreateStaffViewModel model);
        Task<bool> UpdateStaffAsync(int id, EditStaffViewModel model);
        Task<bool> DeleteStaffAsync(int id);
        Task<bool> StaffExistsAsync(int id);

        // Role Management
        Task<StaffRoleViewModel> GetStaffRolesAsync(int staffId);
        Task<bool> AssignRoleToStaffAsync(int staffId, int roleId);
        Task<bool> RemoveRoleFromStaffAsync(int staffId, int roleId);
        Task<IEnumerable<Role>> GetAvailableStaffRolesAsync();

        // Department Management
        Task<IEnumerable<DepartmentEntity>> GetAllDepartmentsAsync();
        Task<IEnumerable<StaffInfo>> GetStaffsByDepartmentAsync(int departmentId);
        Task<DepartmentEntity?> GetDepartmentByIdAsync(int departmentId);

        // Advanced Operations
        Task<IEnumerable<StaffInfo>> SearchStaffsAsync(string searchTerm);
        Task<int> GetTotalStaffCountAsync();

        // Validation Methods
        Task<bool> ValidateStaffDataAsync(CreateStaffViewModel model);
        Task<bool> ValidateStaffUpdateAsync(int id, EditStaffViewModel model);
        Task<bool> CanDeleteStaffAsync(int id);
    }
}