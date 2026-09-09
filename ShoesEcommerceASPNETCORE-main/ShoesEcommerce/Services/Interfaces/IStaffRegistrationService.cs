using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.ViewModels.Account;

namespace ShoesEcommerce.Services.Interfaces
{
    /// <summary>
    /// Service interface for handling complete staff registration process
    /// Following SOLID principles - Single Responsibility: Only handles staff registration logic
    /// Separation of Concerns: Service validates & orchestrates, Repository handles data
    /// </summary>
    public interface IStaffRegistrationService
    {
        /// <summary>
        /// Complete staff registration process including:
        /// - Validating registration data
        /// - Creating staff account
        /// - Assigning role
        /// All within a transaction (handled by repository)
        /// </summary>
        /// <param name="model">Staff registration view model</param>
        /// <returns>Registration result with staff and status information</returns>
        Task<StaffRegistrationResult> RegisterStaffAsync(RegisterStaffViewModel model);

        /// <summary>
        /// Ensures the specified staff role exists in the system
        /// Creates role if it doesn't exist
        /// </summary>
        /// <param name="roleName">Name of the role (Admin/Manager/Staff)</param>
        /// <returns>The role entity</returns>
        Task<Role> EnsureStaffRoleExistsAsync(string roleName);

        /// <summary>
        /// Validates staff registration data
        /// Business logic validation (e.g., email format, phone format, role validity)
        /// </summary>
        /// <param name="model">Staff registration view model</param>
        /// <returns>True if validation passed, false otherwise</returns>
        Task<bool> ValidateStaffRegistrationDataAsync(RegisterStaffViewModel model);
    }
}
