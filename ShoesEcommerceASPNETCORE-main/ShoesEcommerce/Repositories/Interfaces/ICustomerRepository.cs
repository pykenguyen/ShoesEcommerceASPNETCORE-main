using ShoesEcommerce.Models.Accounts;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        // CRUD Operations
        Task<IEnumerable<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<Customer?> GetCustomerByEmailAsync(string email);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(int id);
        Task<bool> CustomerExistsAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> PhoneExistsAsync(string phoneNumber);

        // Registration with Transaction
        Task<Customer?> RegisterCustomerWithCartAndRoleAsync(Customer customer, string roleName = "Customer");

        // Role Management
        Task<IEnumerable<Role>> GetCustomerRolesAsync(int customerId);
        Task<bool> AssignRoleToCustomerAsync(int customerId, int roleId);
        Task<bool> RemoveRoleFromCustomerAsync(int customerId, int roleId);

        // Advanced Queries
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
        Task<IEnumerable<Customer>> GetCustomersWithRolesAsync();
        Task<int> GetTotalCustomerCountAsync();
        Task<IEnumerable<Customer>> GetPaginatedCustomersAsync(int pageNumber, int pageSize);
        Task<IEnumerable<Customer>> GetCustomersByRegistrationDateAsync(DateTime fromDate, DateTime toDate);

        // Authentication specific
        Task<Customer?> ValidateCustomerAsync(string email, string password);
        Task<bool> UpdatePasswordAsync(int customerId, string passwordHash);
    }
}