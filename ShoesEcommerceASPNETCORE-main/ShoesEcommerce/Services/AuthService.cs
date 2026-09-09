using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Account;

namespace ShoesEcommerce.Services
{
    public class AuthService : IAuthService
    {
        private readonly ICustomerService _customerService;
        private readonly IStaffService _staffService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        private readonly ICustomerRegistrationService _customerRegistrationService;

        public AuthService(
            ICustomerService customerService,
            IStaffService staffService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger,
            ICustomerRegistrationService customerRegistrationService)
        {
            _customerService = customerService;
            _staffService = staffService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _customerRegistrationService = customerRegistrationService;
        }

        // Customer Authentication
        public async Task<CustomerLoginResult> AuthenticateCustomerAsync(LoginViewModel model)
        {
            try
            {
                var result = await _customerService.LoginCustomerAsync(model);
                
                if (result.Success && result.Customer != null)
                {
                    var claimsPrincipal = await CreateCustomerClaimsPrincipalAsync(result.Customer);
                    
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? 
                            DateTimeOffset.UtcNow.AddDays(30) : 
                            DateTimeOffset.UtcNow.AddHours(24)
                    };

                    await _httpContextAccessor.HttpContext!.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        claimsPrincipal,
                        authProperties);

                    _logger.LogInformation("Customer authenticated successfully: {Email}", model.Email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating customer: {Email}", model.Email);
                return new CustomerLoginResult
                {
                    Success = false,
                    ErrorMessage = "Có l?i x?y ra trong quá trình ??ng nh?p"
                };
            }
        }

        public async Task<CustomerRegistrationResult> RegisterCustomerAsync(RegisterViewModel model)
        {
            try
            {
                var result = await _customerRegistrationService.RegisterCustomerWithCartAsync(model);
                
                if (result.Success && result.Customer != null)
                {
                    // Automatically sign in the customer after successful registration
                    await SignInCustomerAsync(result.Customer.Id);

                    _logger.LogInformation("Customer registered and authenticated: {Email} with Cart ID: {CartId}", 
                        model.Email, result.Customer.CartId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering customer: {Email}", model.Email);
                return new CustomerRegistrationResult
                {
                    Success = false,
                    ErrorMessage = "Có l?i x?y ra trong quá trình ??ng ký"
                };
            }
        }

        public async Task<CustomerRegistrationResult> RegisterCustomerWithGoogleAsync(RegisterViewModel model, string? googleId, string? profilePicture)
        {
            try
            {
                _logger.LogInformation("Registering customer with Google OAuth: {Email}, GoogleId: {GoogleId}", model.Email, googleId);

                var result = await _customerRegistrationService.RegisterCustomerWithGoogleAsync(model, googleId, profilePicture);
                
                if (result.Success && result.Customer != null)
                {
                    // Automatically sign in the customer
                    await SignInCustomerAsync(result.Customer.Id);

                    _logger.LogInformation("Customer registered via Google and signed in: {Email}, CustomerId: {CustomerId}", 
                        model.Email, result.Customer.Id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering customer with Google: {Email}", model.Email);
                return new CustomerRegistrationResult
                {
                    Success = false,
                    ErrorMessage = "Có l?i x?y ra trong quá trình ??ng ký v?i Google"
                };
            }
        }

        public async Task SignInCustomerAsync(int customerId)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(customerId);
                if (customer == null)
                {
                    _logger.LogWarning("Cannot sign in - customer not found: {CustomerId}", customerId);
                    return;
                }

                var claimsPrincipal = await CreateCustomerClaimsPrincipalAsync(customer);
                
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };

                await _httpContextAccessor.HttpContext!.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    authProperties);

                _logger.LogInformation("Customer signed in: {CustomerId} - {Email}", customerId, customer.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing in customer: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<bool> SignOutAsync()
        {
            try
            {
                await _httpContextAccessor.HttpContext!.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                
                _logger.LogInformation("User signed out successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing out user");
                return false;
            }
        }

        // Staff Authentication
        public async Task<StaffLoginResult> AuthenticateStaffAsync(StaffLoginViewModel model)
        {
            try
            {
                // Implement staff authentication
                var result = await _staffService.LoginStaffAsync(model);
                
                if (result.Success && result.Staff != null)
                {
                    var claimsPrincipal = await CreateStaffClaimsPrincipalAsync(result.Staff);
                    
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? 
                            DateTimeOffset.UtcNow.AddDays(30) : 
                            DateTimeOffset.UtcNow.AddHours(24)
                    };

                    await _httpContextAccessor.HttpContext!.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        claimsPrincipal,
                        authProperties);

                    _logger.LogInformation("Staff authenticated successfully: {Email}", model.Email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating staff: {Email}", model.Email);
                return new StaffLoginResult
                {
                    Success = false,
                    ErrorMessage = "Có l?i x?y ra trong quá trình ??ng nh?p"
                };
            }
        }

        public async Task<bool> ValidateStaffAsync(string email, string password)
        {
            try
            {
                // Implementation for staff validation
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating staff: {Email}", email);
                return false;
            }
        }

        // Claims and Identity
        public async Task<ClaimsPrincipal> CreateCustomerClaimsPrincipalAsync(Customer customer)
        {
            var claims = await GetCustomerClaimsAsync(customer);
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(claimsIdentity);
        }

        public async Task<ClaimsPrincipal> CreateStaffClaimsPrincipalAsync(Staff staff)
        {
            var claims = await GetStaffClaimsAsync(staff);
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(claimsIdentity);
        }

        public async Task<List<Claim>> GetCustomerClaimsAsync(Customer customer)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                new Claim(ClaimTypes.Name, customer.Email),
                new Claim(ClaimTypes.Email, customer.Email),
                new Claim(ClaimTypes.GivenName, customer.FirstName),
                new Claim(ClaimTypes.Surname, customer.LastName),
                new Claim("UserType", "Customer"),
                new Claim("FullName", $"{customer.FirstName} {customer.LastName}".Trim())
            };

            // Add role claims
            if (customer.Roles != null && customer.Roles.Any())
            {
                foreach (var userRole in customer.Roles)
                {
                    if (userRole.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                    }
                }
            }
            else
            {
                // Default role for customers
                claims.Add(new Claim(ClaimTypes.Role, "Customer"));
            }

            return claims;
        }

        public async Task<List<Claim>> GetStaffClaimsAsync(Staff staff)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, staff.Id.ToString()),
                new Claim(ClaimTypes.Name, staff.Email),
                new Claim(ClaimTypes.Email, staff.Email),
                new Claim(ClaimTypes.GivenName, staff.FirstName),
                new Claim(ClaimTypes.Surname, staff.LastName),
                new Claim("UserType", "Staff"),
                new Claim("FullName", staff.FullName),
                new Claim("DepartmentId", staff.DepartmentId.ToString())
            };

            // Add role claims
            if (staff.Roles != null && staff.Roles.Any())
            {
                foreach (var userRole in staff.Roles)
                {
                    if (userRole.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                    }
                }
            }

            // Add department claim
            if (staff.Department != null)
            {
                claims.Add(new Claim("Department", staff.Department.Name));
            }

            return claims;
        }

        // Session Management
        public async Task<bool> RefreshUserClaimsAsync(int userId, string userType)
        {
            try
            {
                ClaimsPrincipal? newPrincipal = null;

                if (userType == "Customer")
                {
                    var customer = await _customerService.GetCustomerByIdAsync(userId);
                    if (customer != null)
                    {
                        newPrincipal = await CreateCustomerClaimsPrincipalAsync(customer);
                    }
                }
                else if (userType == "Staff")
                {
                    var staff = await _staffService.GetStaffByIdAsync(userId);
                    if (staff != null)
                    {
                        newPrincipal = await CreateStaffClaimsPrincipalAsync(staff);
                    }
                }

                if (newPrincipal != null)
                {
                    await _httpContextAccessor.HttpContext!.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        newPrincipal);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing user claims: {UserId}, {UserType}", userId, userType);
                return false;
            }
        }

        public async Task<int?> GetCurrentCustomerIdAsync()
        {
            try
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userType = user.FindFirst("UserType")?.Value;
                    if (userType == "Customer")
                    {
                        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (int.TryParse(userIdClaim, out int customerId))
                        {
                            return customerId;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current customer ID");
                return null;
            }
        }

        public async Task<int?> GetCurrentStaffIdAsync()
        {
            try
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userType = user.FindFirst("UserType")?.Value;
                    if (userType == "Staff")
                    {
                        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (int.TryParse(userIdClaim, out int staffId))
                        {
                            return staffId;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current staff ID");
                return null;
            }
        }

        public async Task<Customer?> GetCurrentCustomerAsync()
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();
                if (customerId.HasValue)
                {
                    return await _customerService.GetCustomerByIdAsync(customerId.Value);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current customer");
                return null;
            }
        }

        public async Task<Staff?> GetCurrentStaffAsync()
        {
            try
            {
                var staffId = await GetCurrentStaffIdAsync();
                if (staffId.HasValue)
                {
                    return await _staffService.GetStaffByIdAsync(staffId.Value);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current staff");
                return null;
            }
        }

        // Password Management
        public async Task<bool> ChangePasswordAsync(int userId, string userType, ChangePasswordViewModel model)
        {
            try
            {
                if (userType == "Customer")
                {
                    // First validate current password
                    var customer = await _customerService.GetCustomerByIdAsync(userId);
                    if (customer == null)
                        return false;

                    if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, customer.PasswordHash))
                        return false;

                    // Update password
                    return await _customerService.UpdatePasswordAsync(userId, model.NewPassword);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password: {UserId}, {UserType}", userId, userType);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string userType)
        {
            // Implementation for password reset email
            // This would typically involve generating a reset token and sending an email
            return false;
        }

        public async Task<bool> ConfirmPasswordResetAsync(string token, string email, string newPassword)
        {
            // Implementation for confirming password reset
            return false;
        }

        // Account Verification
        public async Task<bool> SendVerificationEmailAsync(string email)
        {
            // Implementation for sending verification email
            return false;
        }

        public async Task<bool> VerifyEmailAsync(string token, string email)
        {
            // Implementation for email verification
            return false;
        }

        // Two-Factor Authentication
        public async Task<bool> EnableTwoFactorAsync(int userId, string userType)
        {
            // Implementation for enabling 2FA
            return false;
        }

        public async Task<bool> DisableTwoFactorAsync(int userId, string userType)
        {
            // Implementation for disabling 2FA
            return false;
        }

        public async Task<string> GenerateTwoFactorTokenAsync(int userId, string userType)
        {
            // Implementation for generating 2FA token
            return string.Empty;
        }

        public async Task<bool> VerifyTwoFactorTokenAsync(int userId, string userType, string token)
        {
            // Implementation for verifying 2FA token
            return false;
        }

        // Account Lockout
        public async Task<bool> IsAccountLockedOutAsync(string email, string userType)
        {
            // Implementation for checking account lockout
            return false;
        }

        public async Task<bool> LockAccountAsync(string email, string userType, TimeSpan lockoutDuration)
        {
            // Implementation for locking account
            return false;
        }

        public async Task<bool> UnlockAccountAsync(string email, string userType)
        {
            // Implementation for unlocking account
            return false;
        }

        public async Task<DateTime?> GetAccountLockoutEndAsync(string email, string userType)
        {
            // Implementation for getting lockout end time
            return null;
        }

        // Security
        public async Task<bool> ValidateSecurityStampAsync(int userId, string userType, string securityStamp)
        {
            // Implementation for validating security stamp
            return true;
        }

        public async Task<string> GenerateSecurityStampAsync(int userId, string userType)
        {
            // Implementation for generating security stamp
            return Guid.NewGuid().ToString();
        }

        public async Task<bool> UpdateSecurityStampAsync(int userId, string userType)
        {
            // Implementation for updating security stamp
            return true;
        }
    }
}