using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.ViewModels.Account;

namespace ShoesEcommerce.Services.Interfaces
{
    public interface ICustomerService
    {
        // Customer Management
        Task<IEnumerable<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<Customer?> GetCustomerByEmailAsync(string email);
        Task<bool> CustomerExistsAsync(int id);
        Task<int> GetTotalCustomerCountAsync();

        // Registration & Authentication
        Task<CustomerRegistrationResult> RegisterCustomerAsync(RegisterViewModel model);
        Task<CustomerLoginResult> LoginCustomerAsync(LoginViewModel model);
        Task<bool> ValidateCustomerAsync(string email, string password);
        Task<bool> UpdatePasswordAsync(int customerId, string newPassword);

        // Profile Management
        Task<CustomerProfileViewModel> GetCustomerProfileAsync(int customerId);
        Task<bool> UpdateCustomerProfileAsync(int customerId, UpdateProfileViewModel model);
        Task<bool> UpdateCustomerAddressAsync(int customerId, UpdateAddressViewModel model);

        // Role Management
        Task<IEnumerable<Role>> GetCustomerRolesAsync(int customerId);
        Task<bool> AssignRoleToCustomerAsync(int customerId, int roleId);
        Task<bool> RemoveRoleFromCustomerAsync(int customerId, int roleId);

        // Search & Filtering
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
        Task<CustomerListViewModel> GetPaginatedCustomersAsync(int page, int pageSize, string searchTerm = "");
        Task<IEnumerable<Customer>> GetCustomersByRegistrationDateAsync(DateTime fromDate, DateTime toDate);

        // Validation Methods
        Task<bool> EmailExistsAsync(string email);
        Task<bool> PhoneExistsAsync(string phoneNumber);
        Task<bool> ValidateRegistrationDataAsync(RegisterViewModel model);
        Task<bool> CanDeleteCustomerAsync(int customerId);

        // Account Management
        Task<bool> DeleteCustomerAsync(int customerId);
        Task<bool> ActivateCustomerAccountAsync(int customerId);
        Task<bool> DeactivateCustomerAccountAsync(int customerId);
    }
}