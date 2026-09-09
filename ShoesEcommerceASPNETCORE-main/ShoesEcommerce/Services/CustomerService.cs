using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Account;

namespace ShoesEcommerce.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<CustomerService> _logger;
        private readonly AppDbContext _context;

        public CustomerService(
            ICustomerRepository customerRepository,
            IFileUploadService fileUploadService,
            ILogger<CustomerService> logger,
            AppDbContext context)
        {
            _customerRepository = customerRepository;
            _fileUploadService = fileUploadService;
            _logger = logger;
            _context = context;
        }

        // Customer Management
        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            try
            {
                return await _customerRepository.GetAllCustomersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                return new List<Customer>();
            }
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            try
            {
                return await _customerRepository.GetCustomerByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by ID: {CustomerId}", id);
                return null;
            }
        }

        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            try
            {
                return await _customerRepository.GetCustomerByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by email: {Email}", email);
                return null;
            }
        }

        public async Task<bool> CustomerExistsAsync(int id)
        {
            try
            {
                return await _customerRepository.CustomerExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if customer exists: {CustomerId}", id);
                return false;
            }
        }

        public async Task<int> GetTotalCustomerCountAsync()
        {
            try
            {
                return await _customerRepository.GetTotalCustomerCountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total customer count");
                return 0;
            }
        }

        // Registration & Authentication
        public async Task<CustomerRegistrationResult> RegisterCustomerAsync(RegisterViewModel model)
        {
            var result = new CustomerRegistrationResult();

            try
            {
                _logger.LogInformation("?? CustomerService.RegisterCustomerAsync started for {Email}", model.Email);
                _logger.LogDebug("?? Input data: FirstName={FirstName}, LastName={LastName}, Phone={Phone}, DateOfBirth={DateOfBirth}, AcceptTerms={AcceptTerms}", 
                    model.FirstName, model.LastName, model.PhoneNumber, model.DateOfBirth, model.AcceptTerms);

                // Validate registration data
                _logger.LogDebug("?? Starting registration data validation...");
                if (!await ValidateRegistrationDataAsync(model))
                {
                    _logger.LogWarning("? Registration data validation failed for {Email}", model.Email);
                    result.ErrorMessage = "D? li?u ??ng ký không h?p l?";
                    return result;
                }
                _logger.LogDebug("? Registration data validation passed");

                // Check if email exists
                _logger.LogDebug("?? Checking email existence for {Email}...", model.Email);
                if (await EmailExistsAsync(model.Email))
                {
                    _logger.LogWarning("? Email already exists: {Email}", model.Email);
                    result.ValidationErrors["Email"] = "Email này ?ã ???c s? d?ng";
                    result.ErrorMessage = "Email ?ã t?n t?i";
                    return result;
                }
                _logger.LogDebug("? Email is available");

                // Check if phone exists
                _logger.LogDebug("?? Checking phone existence for {Phone}...", model.PhoneNumber);
                if (await PhoneExistsAsync(model.PhoneNumber))
                {
                    _logger.LogWarning("? Phone already exists: {Phone}", model.PhoneNumber);
                    result.ValidationErrors["PhoneNumber"] = "S? ?i?n tho?i này ?ã ???c s? d?ng";
                    result.ErrorMessage = "S? ?i?n tho?i ?ã t?n t?i";
                    return result;
                }
                _logger.LogDebug("? Phone number is available");

                // Validate DateOfBirth specifically
                _logger.LogDebug("?? Validating DateOfBirth: {DateOfBirth}", model.DateOfBirth);
                if (model.DateOfBirth == DateTime.MinValue || model.DateOfBirth.Year < 1900)
                {
                    _logger.LogWarning("? Invalid DateOfBirth: {DateOfBirth}", model.DateOfBirth);
                    result.ValidationErrors["DateOfBirth"] = "Ngày sinh không h?p l?";
                    result.ErrorMessage = "Ngày sinh không h?p l?";
                    return result;
                }

                // Create customer entity with proper date handling
                _logger.LogDebug("??? Creating customer entity...");
                var customer = new Customer
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    DateOfBirth = DateTime.SpecifyKind(model.DateOfBirth.Date, DateTimeKind.Utc), // Ensure proper date handling
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _logger.LogDebug("? Customer entity created - Email: {Email}, Phone: {Phone}, DOB: {DateOfBirth}", 
                    customer.Email, customer.PhoneNumber, customer.DateOfBirth);

                _logger.LogDebug("?? Calling repository to create customer...");
                var createdCustomer = await _customerRepository.CreateCustomerAsync(customer);
                _logger.LogInformation("? Customer created successfully with ID: {CustomerId}", createdCustomer.Id);

                // Assign default "Customer" role
                _logger.LogDebug("?? Assigning customer role (ID: 2)...");
                try
                {
                    var roleAssigned = await AssignRoleToCustomerAsync(createdCustomer.Id, 2); // Assuming role ID 2 is "Customer"
                    if (roleAssigned)
                    {
                        _logger.LogDebug("? Customer role assigned successfully");
                    }
                    else
                    {
                        _logger.LogWarning("?? Failed to assign customer role, but continuing...");
                    }
                }
                catch (Exception roleEx)
                {
                    _logger.LogError(roleEx, "? Exception while assigning role, but continuing with registration");
                    // Don't fail the registration if role assignment fails
                }

                // Create empty cart for customer
                try
                {
                    var cart = new ShoesEcommerce.Models.Carts.Cart
                    {
                        Customer = createdCustomer,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                    createdCustomer.Cart = cart;
                    createdCustomer.CartId = cart.Id;
                    await _customerRepository.UpdateCustomerAsync(createdCustomer);
                    _logger.LogInformation("? Cart created for customer {CustomerId}", createdCustomer.Id);
                }
                catch (Exception cartEx)
                {
                    _logger.LogError(cartEx, "? Exception while creating cart for customer, but continuing registration");
                }

                result.Success = true;
                result.Customer = createdCustomer;

                _logger.LogInformation("?? Customer registration completed successfully: {Email} (ID: {CustomerId})", model.Email, createdCustomer.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? CRITICAL ERROR in CustomerService.RegisterCustomerAsync for {Email}. Exception Type: {ExceptionType}, Message: {Message}", 
                    model.Email, ex.GetType().Name, ex.Message);
                _logger.LogError("?? Full Stack Trace: {StackTrace}", ex.StackTrace);
                
                // Log inner exceptions
                var innerEx = ex.InnerException;
                var level = 1;
                while (innerEx != null)
                {
                    _logger.LogError("?? Inner Exception Level {Level}: Type={Type}, Message={Message}", 
                        level, innerEx.GetType().Name, innerEx.Message);
                    innerEx = innerEx.InnerException;
                    level++;
                    if (level > 5) break; // Prevent infinite loops
                }
                
                // Return a proper Vietnamese error message
                result.ErrorMessage = "Có l?i x?y ra trong quá trình ??ng ký. Vui lòng th? l?i sau.";
                
                // Add specific error details for debugging
                if (ex.Message.Contains("datetime") || ex.Message.Contains("DateTime"))
                {
                    result.ErrorMessage = "L?i ??nh d?ng ngày tháng. Vui lòng ki?m tra ngày sinh.";
                }
                else if (ex.Message.Contains("phone") || ex.Message.Contains("Phone"))
                {
                    result.ErrorMessage = "L?i ??nh d?ng s? ?i?n tho?i. Vui lòng ki?m tra s? ?i?n tho?i.";
                }
                else if (ex.Message.Contains("email") || ex.Message.Contains("Email"))
                {
                    result.ErrorMessage = "L?i ??nh d?ng email. Vui lòng ki?m tra ??a ch? email.";
                }
                return result;
            }
        }

        public async Task<CustomerLoginResult> LoginCustomerAsync(LoginViewModel model)
        {
            var result = new CustomerLoginResult();

            try
            {
                // Validate customer credentials
                var customer = await _customerRepository.ValidateCustomerAsync(model.Email, model.Password);
                if (customer == null)
                {
                    result.ErrorMessage = "Email ho?c m?t kh?u không ??ng";
                    return result;
                }

                result.Success = true;
                result.Customer = customer;

                _logger.LogInformation("Customer logged in successfully: {Email}", model.Email);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in customer: {Email}", model.Email);
                result.ErrorMessage = "Có l?i x?y ra trong quá trình ??ng nh?p";
                return result;
            }
        }

        public async Task<bool> ValidateCustomerAsync(string email, string password)
        {
            try
            {
                var customer = await _customerRepository.ValidateCustomerAsync(email, password);
                return customer != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating customer: {Email}", email);
                return false;
            }
        }

        public async Task<bool> UpdatePasswordAsync(int customerId, string newPassword)
        {
            try
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                return await _customerRepository.UpdatePasswordAsync(customerId, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for customer: {CustomerId}", customerId);
                return false;
            }
        }

        // Profile Management
        public async Task<CustomerProfileViewModel> GetCustomerProfileAsync(int customerId)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    return new CustomerProfileViewModel();

                return new CustomerProfileViewModel
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    PhoneNumber = customer.PhoneNumber,
                    DateOfBirth = customer.DateOfBirth,
                    ImageUrl = customer.ImageUrl,
                    Address = customer.Address,
                    City = customer.City,
                    State = customer.State,
                    CreatedAt = customer.CreatedAt,
                    TotalOrders = customer.Orders?.Count ?? 0,
                    TotalSpent = customer.Orders?.Sum(o => o.TotalAmount) ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer profile: {CustomerId}", customerId);
                return new CustomerProfileViewModel();
            }
        }

        public async Task<bool> UpdateCustomerProfileAsync(int customerId, UpdateProfileViewModel model)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    return false;

                customer.FirstName = model.FirstName.Trim();
                customer.LastName = model.LastName.Trim();
                customer.PhoneNumber = model.PhoneNumber.Trim();
                customer.DateOfBirth = model.DateOfBirth;

                // Handle profile image upload
                if (model.ProfileImage != null)
                {
                    var imageUrl = await _fileUploadService.UploadImageAsync(
                        model.ProfileImage,
                        "customers");
                    
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        customer.ImageUrl = imageUrl;
                    }
                }

                await _customerRepository.UpdateCustomerAsync(customer);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer profile: {CustomerId}", customerId);
                return false;
            }
        }

        public async Task<bool> UpdateCustomerAddressAsync(int customerId, UpdateAddressViewModel model)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    return false;

                customer.Address = model.Address?.Trim();
                customer.City = model.City?.Trim();
                customer.State = model.State?.Trim();

                await _customerRepository.UpdateCustomerAsync(customer);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer address: {CustomerId}", customerId);
                return false;
            }
        }

        // Role Management
        public async Task<IEnumerable<Role>> GetCustomerRolesAsync(int customerId)
        {
            try
            {
                return await _customerRepository.GetCustomerRolesAsync(customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer roles: {CustomerId}", customerId);
                return new List<Role>();
            }
        }

        public async Task<bool> AssignRoleToCustomerAsync(int customerId, int roleId)
        {
            try
            {
                return await _customerRepository.AssignRoleToCustomerAsync(customerId, roleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to customer: {CustomerId}, {RoleId}", customerId, roleId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleFromCustomerAsync(int customerId, int roleId)
        {
            try
            {
                return await _customerRepository.RemoveRoleFromCustomerAsync(customerId, roleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from customer: {CustomerId}, {RoleId}", customerId, roleId);
                return false;
            }
        }

        // Search & Filtering
        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            try
            {
                return await _customerRepository.SearchCustomersAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers: {SearchTerm}", searchTerm);
                return new List<Customer>();
            }
        }

        public async Task<CustomerListViewModel> GetPaginatedCustomersAsync(int page, int pageSize, string searchTerm = "")
        {
            try
            {
                var customers = string.IsNullOrEmpty(searchTerm)
                    ? await _customerRepository.GetPaginatedCustomersAsync(page, pageSize)
                    : await _customerRepository.SearchCustomersAsync(searchTerm);

                var totalCount = string.IsNullOrEmpty(searchTerm)
                    ? await _customerRepository.GetTotalCustomerCountAsync()
                    : customers.Count();

                var customerInfos = customers.Select(c => new CustomerInfo
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    CreatedAt = c.CreatedAt,
                    TotalOrders = c.Orders?.Count ?? 0,
                    IsActive = true // You can implement customer status logic
                });

                return new CustomerListViewModel
                {
                    Customers = customerInfos,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated customers: Page {Page}, PageSize {PageSize}", page, pageSize);
                return new CustomerListViewModel();
            }
        }

        public async Task<IEnumerable<Customer>> GetCustomersByRegistrationDateAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return await _customerRepository.GetCustomersByRegistrationDateAsync(fromDate, toDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers by registration date: {FromDate} - {ToDate}", fromDate, toDate);
                return new List<Customer>();
            }
        }

        // Validation Methods
        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _customerRepository.EmailExistsAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email exists: {Email}", email);
                return true; // Return true to be safe (prevent registration)
            }
        }

        public async Task<bool> PhoneExistsAsync(string phoneNumber)
        {
            try
            {
                return await _customerRepository.PhoneExistsAsync(phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if phone exists: {PhoneNumber}", phoneNumber);
                return true; // Return true to be safe (prevent registration)
            }
        }

        public async Task<bool> ValidateRegistrationDataAsync(RegisterViewModel model)
        {
            try
            {
                _logger.LogDebug("?? Starting detailed validation for {Email}", model.Email);
                
                // Basic validation
                if (string.IsNullOrWhiteSpace(model.Email) ||
                    string.IsNullOrWhiteSpace(model.FirstName) ||
                    string.IsNullOrWhiteSpace(model.LastName) ||
                    string.IsNullOrWhiteSpace(model.Password) ||
                    string.IsNullOrWhiteSpace(model.PhoneNumber))
                {
                    _logger.LogWarning("? Basic validation failed: missing required fields");
                    return false;
                }

                // Email format validation
                if (!IsValidEmail(model.Email))
                {
                    _logger.LogWarning("? Email format validation failed: {Email}", model.Email);
                    return false;
                }
                _logger.LogDebug("? Email format valid");

                // Phone format validation - Vietnamese phone numbers
                if (!IsValidVietnamesePhoneNumber(model.PhoneNumber))
                {
                    _logger.LogWarning("? Phone format validation failed: {Phone}", model.PhoneNumber);
                    return false;
                }
                _logger.LogDebug("? Phone format valid");

                // Date of birth validation
                if (model.DateOfBirth == DateTime.MinValue)
                {
                    _logger.LogWarning("? DateOfBirth is MinValue: {DateOfBirth}", model.DateOfBirth);
                    return false;
                }

                if (model.DateOfBirth.Year < 1900 || model.DateOfBirth.Year > DateTime.Now.Year)
                {
                    _logger.LogWarning("? DateOfBirth year is invalid: {Year}", model.DateOfBirth.Year);
                    return false;
                }

                // Age validation (must be at least 13 years old)
                var age = DateTime.Now.Year - model.DateOfBirth.Year;
                if (model.DateOfBirth > DateTime.Now.AddYears(-age)) age--;
                
                _logger.LogDebug("?? Calculated age: {Age} for date {DateOfBirth}", age, model.DateOfBirth);
                
                if (age < 13)
                {
                    _logger.LogWarning("? Age validation failed: age {Age} for date {DateOfBirth}", age, model.DateOfBirth);
                    return false;
                }

                // Future date validation
                if (model.DateOfBirth > DateTime.Now.Date)
                {
                    _logger.LogWarning("? Date of birth is in the future: {DateOfBirth}", model.DateOfBirth);
                    return false;
                }

                // Password strength validation
                if (model.Password.Length < 6)
                {
                    _logger.LogWarning("? Password too short: {Length} characters", model.Password.Length);
                    return false;
                }

                _logger.LogDebug("? All validations passed for {Email}", model.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error validating registration data for {Email}", model.Email);
                return false;
            }
        }

        public async Task<bool> CanDeleteCustomerAsync(int customerId)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    return false;

                // Don't allow deletion if customer has orders
                return customer.Orders == null || !customer.Orders.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if customer can be deleted: {CustomerId}", customerId);
                return false;
            }
        }

        // Account Management
        public async Task<bool> DeleteCustomerAsync(int customerId)
        {
            try
            {
                if (!await CanDeleteCustomerAsync(customerId))
                    return false;

                return await _customerRepository.DeleteCustomerAsync(customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {CustomerId}", customerId);
                return false;
            }
        }

        public async Task<bool> ActivateCustomerAccountAsync(int customerId)
        {
            try
            {
                // Implementation depends on how you handle account status
                // For now, return true as a placeholder
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating customer account: {CustomerId}", customerId);
                return false;
            }
        }

        public async Task<bool> DeactivateCustomerAccountAsync(int customerId)
        {
            try
            {
                // Implementation depends on how you handle account status
                // For now, return true as a placeholder
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating customer account: {CustomerId}", customerId);
                return false;
            }
        }

        // Helper methods
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

        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\+?\d{10,15}$");
        }

        private static bool IsValidVietnamesePhoneNumber(string phoneNumber)
        {
            // Vietnamese phone number patterns:
            // Mobile: 03x, 05x, 07x, 08x, 09x followed by 8 digits
            // Landline: 02x followed by 8-9 digits
            var mobilePattern = @"^(0[3|5|7|8|9])[0-9]{8}$";
            var landlinePattern = @"^(02)[0-9]{8,9}$";
            
            return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, mobilePattern) ||
                   System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, landlinePattern);
        }
    }
}