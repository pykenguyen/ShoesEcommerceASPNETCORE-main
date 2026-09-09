using System.Security.Claims;
using ShoesEcommerce.Models.Accounts;
using ShoesEcommerce.ViewModels.Account;

namespace ShoesEcommerce.Services.Interfaces
{
    public interface IAuthService
    {
        // Customer Authentication
        Task<CustomerLoginResult> AuthenticateCustomerAsync(LoginViewModel model);
        Task<CustomerRegistrationResult> RegisterCustomerAsync(RegisterViewModel model);
        Task<CustomerRegistrationResult> RegisterCustomerWithGoogleAsync(RegisterViewModel model, string? googleId, string? profilePicture);
        Task SignInCustomerAsync(int customerId);
        Task<bool> SignOutAsync();
        
        // Staff Authentication
        Task<StaffLoginResult> AuthenticateStaffAsync(StaffLoginViewModel model);
        Task<bool> ValidateStaffAsync(string email, string password);

        // Claims and Identity
        Task<ClaimsPrincipal> CreateCustomerClaimsPrincipalAsync(Customer customer);
        Task<ClaimsPrincipal> CreateStaffClaimsPrincipalAsync(Staff staff);
        Task<List<Claim>> GetCustomerClaimsAsync(Customer customer);
        Task<List<Claim>> GetStaffClaimsAsync(Staff staff);

        // Session Management
        Task<bool> RefreshUserClaimsAsync(int userId, string userType);
        Task<int?> GetCurrentCustomerIdAsync();
        Task<int?> GetCurrentStaffIdAsync();
        Task<Customer?> GetCurrentCustomerAsync();
        Task<Staff?> GetCurrentStaffAsync();

        // Password Management
        Task<bool> ChangePasswordAsync(int userId, string userType, ChangePasswordViewModel model);
        Task<bool> ResetPasswordAsync(string email, string userType);
        Task<bool> ConfirmPasswordResetAsync(string token, string email, string newPassword);

        // Account Verification
        Task<bool> SendVerificationEmailAsync(string email);
        Task<bool> VerifyEmailAsync(string token, string email);
        
        // Two-Factor Authentication
        Task<bool> EnableTwoFactorAsync(int userId, string userType);
        Task<bool> DisableTwoFactorAsync(int userId, string userType);
        Task<string> GenerateTwoFactorTokenAsync(int userId, string userType);
        Task<bool> VerifyTwoFactorTokenAsync(int userId, string userType, string token);

        // Account Lockout
        Task<bool> IsAccountLockedOutAsync(string email, string userType);
        Task<bool> LockAccountAsync(string email, string userType, TimeSpan lockoutDuration);
        Task<bool> UnlockAccountAsync(string email, string userType);
        Task<DateTime?> GetAccountLockoutEndAsync(string email, string userType);

        // Security
        Task<bool> ValidateSecurityStampAsync(int userId, string userType, string securityStamp);
        Task<string> GenerateSecurityStampAsync(int userId, string userType);
        Task<bool> UpdateSecurityStampAsync(int userId, string userType);
    }

    // Additional result classes
    public class StaffLoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;
        public string? ReturnUrl { get; set; }
    }
}