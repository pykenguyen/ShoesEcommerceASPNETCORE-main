using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Account;
using ShoesEcommerce.ViewModels.Staff;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;

namespace ShoesEcommerce.Services
{
    public class StaffService : IStaffService
    {
        private readonly IStaffRepository _staffRepository;
        private readonly AppDbContext _context;
        private readonly ILogger<StaffService> _logger;

        public StaffService(
            IStaffRepository staffRepository,
            AppDbContext context,
            ILogger<StaffService> logger)
        {
            _staffRepository = staffRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<StaffListViewModel> GetStaffsAsync(string searchTerm, int? departmentId, int page, int pageSize)
        {
            // Basic implementation - you can enhance this later
            return new StaffListViewModel();
        }

        public async Task<Staff?> GetStaffByIdAsync(int id)
        {
            return await _staffRepository.GetStaffByIdAsync(id);
        }

        public async Task<StaffInfo> CreateStaffAsync(CreateStaffViewModel model)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateStaffAsync(int id, EditStaffViewModel model)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteStaffAsync(int id)
        {
            return await _staffRepository.DeleteStaffAsync(id);
        }

        public async Task<bool> StaffExistsAsync(int id)
        {
            return await _staffRepository.StaffExistsAsync(id);
        }

        public async Task<StaffRoleViewModel> GetStaffRolesAsync(int staffId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> AssignRoleToStaffAsync(int staffId, int roleId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RemoveRoleFromStaffAsync(int staffId, int roleId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Role>> GetAvailableStaffRolesAsync()
        {
            return new List<Role>();
        }

        public async Task<IEnumerable<DepartmentEntity>> GetAllDepartmentsAsync()
        {
            return await _staffRepository.GetAllDepartmentsAsync();
        }

        public async Task<IEnumerable<StaffInfo>> GetStaffsByDepartmentAsync(int departmentId)
        {
            return new List<StaffInfo>();
        }

        public async Task<DepartmentEntity?> GetDepartmentByIdAsync(int departmentId)
        {
            return await _staffRepository.GetDepartmentByIdAsync(departmentId);
        }

        public async Task<IEnumerable<StaffInfo>> SearchStaffsAsync(string searchTerm)
        {
            return new List<StaffInfo>();
        }

        public async Task<int> GetTotalStaffCountAsync()
        {
            return await _staffRepository.GetTotalStaffCountAsync();
        }

        public async Task<bool> ValidateStaffDataAsync(CreateStaffViewModel model)
        {
            return true; // Basic validation
        }

        public async Task<bool> ValidateStaffUpdateAsync(int id, EditStaffViewModel model)
        {
            return true; // Basic validation
        }

        public async Task<bool> CanDeleteStaffAsync(int id)
        {
            return true; // Basic check
        }

        // Authentication Methods
        public async Task<StaffLoginResult> LoginStaffAsync(StaffLoginViewModel model)
        {
            var result = new StaffLoginResult();

            try
            {
                // Validate staff credentials
                var staff = await _staffRepository.ValidateStaffAsync(model.Email, model.Password);
                if (staff == null)
                {
                    result.ErrorMessage = "Email ho?c m?t kh?u không ?úng";
                    return result;
                }

                // Load staff with roles
                var fullStaff = await _context.Staffs
                    .Include(s => s.Roles)
                        .ThenInclude(ur => ur.Role)
                    .Include(s => s.Department)
                    .FirstOrDefaultAsync(s => s.Id == staff.Id);

                result.Success = true;
                result.Staff = fullStaff;

                _logger.LogInformation("Staff logged in successfully: {Email}", model.Email);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in staff: {Email}", model.Email);
                result.ErrorMessage = "Có l?i x?y ra trong quá trình ??ng nh?p";
                return result;
            }
        }

        public async Task<bool> ValidateStaffAsync(string email, string password)
        {
            try
            {
                var staff = await _staffRepository.ValidateStaffAsync(email, password);
                return staff != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating staff: {Email}", email);
                return false;
            }
        }
    }
}