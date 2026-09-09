using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Account;

namespace ShoesEcommerce.Services
{
    /// <summary>
    /// Service for handling complete staff registration process
    /// Following SOLID principles:
    /// - Single Responsibility: Only handles staff registration orchestration
    /// - Separation of Concerns: Service validates, Repository handles data
    /// - Dependency Inversion: Depends on abstractions (interfaces)
    /// </summary>
    public class StaffRegistrationService : IStaffRegistrationService
    {
        private readonly IStaffRepository _staffRepository;
        private readonly ILogger<StaffRegistrationService> _logger;
        private readonly AppDbContext _context; // For role operations only

        public StaffRegistrationService(
            IStaffRepository staffRepository,
            ILogger<StaffRegistrationService> logger,
            AppDbContext context)
        {
            _staffRepository = staffRepository;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Complete staff registration process
        /// Service layer responsibilities:
        /// 1. Validate business rules
        /// 2. Check data constraints (email/phone exists)
        /// 3. Create entity from ViewModel
        /// 4. Call repository for data operations (transaction handled there)
        /// 5. Return result
        /// </summary>
        public async Task<StaffRegistrationResult> RegisterStaffAsync(RegisterStaffViewModel model)
        {
            var result = new StaffRegistrationResult();

            try
            {
                _logger.LogInformation("?? Starting staff registration for {Email} with role {RoleName}", 
                    model.Email, model.RoleName);

                // ===== STEP 1: VALIDATION (Service Layer Responsibility) =====

                // Validate registration data (business logic)
                if (!await ValidateStaffRegistrationDataAsync(model))
                {
                    _logger.LogWarning("?? Validation failed for {Email}", model.Email);
                    result.Success = false;
                    result.ErrorMessage = "D? li?u ??ng ký không h?p l?.";
                    return result;
                }

                // Check if email already exists
                if (await _staffRepository.EmailExistsAsync(model.Email))
                {
                    _logger.LogWarning("?? Email already exists: {Email}", model.Email);
                    result.Success = false;
                    result.ErrorMessage = "Email ?ã ???c s? d?ng.";
                    result.AddValidationError("Email", "Email ?ã ???c s? d?ng.");
                    return result;
                }

                // Check if phone already exists
                if (await _staffRepository.PhoneExistsAsync(model.PhoneNumber))
                {
                    _logger.LogWarning("?? Phone already exists: {Phone}", model.PhoneNumber);
                    result.Success = false;
                    result.ErrorMessage = "S? ?i?n tho?i ?ã ???c s? d?ng.";
                    result.AddValidationError("PhoneNumber", "S? ?i?n tho?i ?ã ???c s? d?ng.");
                    return result;
                }

                // Validate department exists
                var department = await _context.Departments.FindAsync(model.DepartmentId);
                if (department == null)
                {
                    _logger.LogWarning("?? Department not found: {DepartmentId}", model.DepartmentId);
                    result.Success = false;
                    result.ErrorMessage = "Phòng ban không t?n t?i.";
                    result.AddValidationError("DepartmentId", "Phòng ban không t?n t?i.");
                    return result;
                }

                // Validate role name
                if (!IsValidStaffRole(model.RoleName))
                {
                    _logger.LogWarning("?? Invalid role name: {RoleName}", model.RoleName);
                    result.Success = false;
                    result.ErrorMessage = "Vai trò không h?p l?.";
                    result.AddValidationError("RoleName", "Vai trò ph?i là Admin, Manager, ho?c Staff.");
                    return result;
                }

                // ===== STEP 2: CREATE ENTITY (Service Layer Responsibility) =====

                var staff = new Staff
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    DepartmentId = model.DepartmentId
                };

                _logger.LogDebug("?? Staff entity created: {Email}, Department: {DepartmentId}", 
                    staff.Email, staff.DepartmentId);

                // ===== STEP 3: CALL REPOSITORY (Repository handles transaction) =====

                var createdStaff = await _staffRepository.RegisterStaffWithRoleAsync(staff, model.RoleName);

                if (createdStaff == null)
                {
                    throw new Exception("Repository failed to create staff");
                }

                // ===== STEP 4: RETURN SUCCESS RESULT =====

                result.Success = true;
                result.Staff = createdStaff;

                _logger.LogInformation("?? Staff registration successful for {Email} - Staff ID: {StaffId}, Role: {RoleName}", 
                    model.Email, createdStaff.Id, model.RoleName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error in staff registration for {Email}", model.Email);

                result.Success = false;
                result.ErrorMessage = "Có l?i x?y ra trong quá trình t?o tài kho?n nhân viên. Vui lòng th? l?i sau.";
                return result;
            }
        }

        /// <summary>
        /// Ensures the specified staff role exists in the system
        /// Creates role if it doesn't exist
        /// </summary>
        public async Task<Role> EnsureStaffRoleExistsAsync(string roleName)
        {
            try
            {
                _logger.LogDebug("?? Checking if Staff role exists: {RoleName}", roleName);

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == roleName && r.UserType == UserType.Staff);

                if (role == null)
                {
                    _logger.LogInformation("? Creating Staff role: {RoleName}", roleName);
                    role = new Role
                    {
                        Name = roleName,
                        UserType = UserType.Staff
                    };
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("? Staff role created with ID: {RoleId}", role.Id);
                }
                else
                {
                    _logger.LogDebug("? Staff role already exists with ID: {RoleId}", role.Id);
                }

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error ensuring Staff role exists: {RoleName}", roleName);
                throw;
            }
        }

        /// <summary>
        /// Validates staff registration data (business logic validation)
        /// Service layer responsibility: Business rules validation
        /// </summary>
        public async Task<bool> ValidateStaffRegistrationDataAsync(RegisterStaffViewModel model)
        {
            try
            {
                _logger.LogDebug("?? Starting validation for {Email}", model.Email);

                // Basic validation
                if (string.IsNullOrWhiteSpace(model.Email) ||
                    string.IsNullOrWhiteSpace(model.FirstName) ||
                    string.IsNullOrWhiteSpace(model.LastName) ||
                    string.IsNullOrWhiteSpace(model.Password) ||
                    string.IsNullOrWhiteSpace(model.PhoneNumber) ||
                    string.IsNullOrWhiteSpace(model.RoleName))
                {
                    _logger.LogWarning("?? Basic validation failed: missing required fields");
                    return false;
                }

                // Email format validation
                if (!IsValidEmail(model.Email))
                {
                    _logger.LogWarning("?? Email format validation failed: {Email}", model.Email);
                    return false;
                }

                // Phone format validation - Vietnamese phone numbers
                if (!IsValidVietnamesePhoneNumber(model.PhoneNumber))
                {
                    _logger.LogWarning("?? Phone format validation failed: {Phone}", model.PhoneNumber);
                    return false;
                }

                // Password strength validation
                if (model.Password.Length < 6)
                {
                    _logger.LogWarning("?? Password too short: {Length} characters", model.Password.Length);
                    return false;
                }

                // Department ID validation
                if (model.DepartmentId <= 0)
                {
                    _logger.LogWarning("?? Invalid DepartmentId: {DepartmentId}", model.DepartmentId);
                    return false;
                }

                _logger.LogDebug("? All validations passed for {Email}", model.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error validating staff registration data for {Email}", model.Email);
                return false;
            }
        }

        // ===== PRIVATE HELPER METHODS (Business Logic) =====

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidVietnamesePhoneNumber(string phoneNumber)
        {
            // Vietnamese phone number patterns:
            // Mobile: 03x, 05x, 07x, 08x, 09x followed by 8 digits
            var mobilePattern = @"^(0[3|5|7|8|9])[0-9]{8}$";
            return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, mobilePattern);
        }

        private static bool IsValidStaffRole(string roleName)
        {
            var validRoles = new[] { "Admin", "Manager", "Staff" };
            return validRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
        }
    }
}
