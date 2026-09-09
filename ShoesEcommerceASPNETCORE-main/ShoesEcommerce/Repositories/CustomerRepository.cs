using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Repositories.Interfaces;
using System.Threading.Tasks;

namespace ShoesEcommerce.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        // ? NEW: Complete customer registration with cart and role in a single transaction
        public async Task<Customer?> RegisterCustomerWithCartAndRoleAsync(Customer customer, string roleName = "Customer")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Step 1: Add customer
                customer.CreatedAt = DateTime.UtcNow;
                customer.UpdatedAt = DateTime.UtcNow;
                _context.Customers.Add(customer);
                
                // Step 2: Create cart
                var cart = new Cart
                {
                    SessionId = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CartItems = new List<CartItem>()
                };
                _context.Carts.Add(cart);
                
                // Step 3: Save to generate IDs
                await _context.SaveChangesAsync();
                
                // Step 4: Link cart to customer
                customer.CartId = cart.Id;
                customer.Cart = cart;
                await _context.SaveChangesAsync();
                
                // Step 5: Assign role
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == roleName && r.UserType == UserType.Customer);
                
                if (role != null)
                {
                    var userRole = new UserRole
                    {
                        CustomerId = customer.Id,
                        RoleId = role.Id
                    };
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }
                
                await transaction.CommitAsync();
                return customer;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // CRUD Operations
        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers
                .Include(c => c.Roles)
                    .ThenInclude(ur => ur.Role)
                .Include(c => c.Orders)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.Roles)
                    .ThenInclude(ur => ur.Role)
                .Include(c => c.Orders)
                .Include(c => c.ShippingAddresses)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            return await _context.Customers
                .Include(c => c.Roles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            customer.UpdatedAt = DateTime.UtcNow;

            _context.Entry(customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return customer;
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return false;

            // Remove associated UserRoles first
            var userRoles = await _context.UserRoles
                .Where(ur => ur.CustomerId == id)
                .ToListAsync();
            _context.UserRoles.RemoveRange(userRoles);

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CustomerExistsAsync(int id)
        {
            return await _context.Customers.AnyAsync(c => c.Id == id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Customers.AnyAsync(c => c.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> PhoneExistsAsync(string phoneNumber)
        {
            return await _context.Customers.AnyAsync(c => c.PhoneNumber == phoneNumber);
        }

        // Role Management
        public async Task<IEnumerable<Role>> GetCustomerRolesAsync(int customerId)
        {
            return await _context.UserRoles
                .Where(ur => ur.CustomerId == customerId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<bool> AssignRoleToCustomerAsync(int customerId, int roleId)
        {
            // Check if role assignment already exists
            var exists = await _context.UserRoles
                .AnyAsync(ur => ur.CustomerId == customerId && ur.RoleId == roleId);

            if (exists)
                return false;

            var userRole = new UserRole
            {
                CustomerId = customerId,
                RoleId = roleId
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleFromCustomerAsync(int customerId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.CustomerId == customerId && ur.RoleId == roleId);

            if (userRole == null)
                return false;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        // Advanced Queries
        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllCustomersAsync();

            searchTerm = searchTerm.ToLower();

            return await _context.Customers
                .Include(c => c.Roles)
                    .ThenInclude(ur => ur.Role)
                .Where(c =>
                    c.FirstName.ToLower().Contains(searchTerm) ||
                    c.LastName.ToLower().Contains(searchTerm) ||
                    c.Email.ToLower().Contains(searchTerm) ||
                    c.PhoneNumber.Contains(searchTerm) ||
                    (c.FirstName + " " + c.LastName).ToLower().Contains(searchTerm))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> GetCustomersWithRolesAsync()
        {
            return await _context.Customers
                .Include(c => c.Roles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<int> GetTotalCustomerCountAsync()
        {
            return await _context.Customers.CountAsync();
        }

        public async Task<IEnumerable<Customer>> GetPaginatedCustomersAsync(int pageNumber, int pageSize)
        {
            return await _context.Customers
                .Include(c => c.Roles)
                    .ThenInclude(ur => ur.Role)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> GetCustomersByRegistrationDateAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Customers
                .Include(c => c.Roles)
                    .ThenInclude(ur => ur.Role)
                .Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // Authentication specific
        public async Task<Customer?> ValidateCustomerAsync(string email, string password)
        {
            var customer = await GetCustomerByEmailAsync(email);
            if (customer == null)
                return null;

            // Verify password hash - you'll need to implement password hashing
            if (BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash))
            {
                return customer;
            }

            return null;
        }

        public async Task<bool> UpdatePasswordAsync(int customerId, string passwordHash)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
                return false;

            customer.PasswordHash = passwordHash;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }
    }
}