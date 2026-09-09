using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Account;
using System.Security.Claims;
using ShoesEcommerce.Models.ViewModels;
using ShoesEcommerce.Helpers;

namespace ShoesEcommerce.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ICustomerService _customerService;
        private readonly ILogger<AccountController> _logger;
        private readonly ICustomerRegistrationService _registrationService;
        private readonly ITwilioService _twilioService;

        public AccountController(
            IAuthService authService,
            ICustomerService customerService,
            ILogger<AccountController> logger,
            ICustomerRegistrationService registrationService,
            ITwilioService twilioService)
        {
            _authService = authService;
            _customerService = customerService;
            _logger = logger;
            _registrationService = registrationService;
            _twilioService = twilioService;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = "/")
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(returnUrl);
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Try customer login first
                var customerResult = await _authService.AuthenticateCustomerAsync(model);

                if (customerResult.Success)
                {
                    _logger.LogInformation("Customer {Email} logged in successfully", model.Email);

                    // Clean return URL to avoid routing issues
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) && !model.ReturnUrl.Contains("Admin"))
                    {
                        return LocalRedirect(model.ReturnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }

                // If customer login failed, try staff login
                var staffLoginModel = new StaffLoginViewModel
                {
                    Email = model.Email,
                    Password = model.Password,
                    RememberMe = model.RememberMe,
                    ReturnUrl = model.ReturnUrl
                };

                var staffResult = await _authService.AuthenticateStaffAsync(staffLoginModel);

                if (staffResult.Success)
                {
                    _logger.LogInformation("Staff {Email} logged in successfully", model.Email);

                    // Staff users should go to admin area by default
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return LocalRedirect(model.ReturnUrl);
                    }

                    return RedirectToAction("Index", "Admin", new { area = "Admin" });
                }

                // Both failed - show generic error
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng");
                _logger.LogWarning("Failed login attempt for {Email}: Both customer and staff login failed", model.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra trong quá trình đăng nhập. Vui lòng thử lại.");
            }

            return View(model);
        }

        // GET: /Account/Register
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = "/")
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(returnUrl);
            }

            var model = new RegisterViewModel();
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = "/")
        {
            try
            {
                _logger.LogInformation("🚀 Registration attempt started for email: {Email}", model.Email);
                _logger.LogDebug("Registration model received: DateOfBirth={DateOfBirth}", model.DateOfBirth);

                // *** FIX: Manual DateOfBirth validation ***
                int calculatedAge = 0;
                if (model.DateOfBirth == DateTime.MinValue)
                {
                    ModelState.AddModelError("DateOfBirth", "Vui lòng chọn ngày sinh của bạn.");
                }
                else
                {
                    var today = DateTime.Today;
                    calculatedAge = today.Year - model.DateOfBirth.Year;
                    if (model.DateOfBirth.Date > today.AddYears(-calculatedAge)) calculatedAge--;
                    _logger.LogDebug("🔍 Age validation: calculated age = {Age}", calculatedAge);
                    if (calculatedAge < 13)
                    {
                        ModelState.AddModelError("DateOfBirth", "Bạn phải đủ 13 tuổi để đăng ký.");
                        _logger.LogWarning("❌ Age validation failed: age {Age} is below minimum", calculatedAge);
                    }
                }
                // *** END FIX ***

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("❌ Model validation failed for registration: {Email}", model.Email);
                    foreach (var error in ModelState)
                    {
                        if (error.Value.Errors.Any())
                        {
                            _logger.LogWarning("Validation error for {Field}: {Errors}", error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                        }
                    }
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                _logger.LogInformation("✅ Model validation passed, starting server-side validation...");

                // Additional server-side validation
                var validationErrors = new Dictionary<string, string>();

                _logger.LogDebug("🔍 Checking if email exists: {Email}", model.Email);
                // Check if email already exists
                if (await _customerService.EmailExistsAsync(model.Email))
                {
                    validationErrors.Add("Email", "Email này đã được sử dụng");
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    _logger.LogWarning("❌ Email already exists: {Email}", model.Email);
                }

                _logger.LogDebug("🔍 Checking if phone exists: {Phone}", model.PhoneNumber);
                // Check if phone already exists
                if (await _customerService.PhoneExistsAsync(model.PhoneNumber))
                {
                    validationErrors.Add("PhoneNumber", "Số điện thoại này đã được sử dụng");
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng");
                    _logger.LogWarning("❌ Phone already exists: {Phone}", model.PhoneNumber);
                }

                // Age validation
                var age = DateTime.Now.Year - model.DateOfBirth.Year;
                if (model.DateOfBirth > DateTime.Now.AddYears(-age)) age--;
                _logger.LogDebug("🔍 Age validation: calculated age = {Age}", age);

                if (age < 13)
                {
                    validationErrors.Add("DateOfBirth", "Bạn phải đủ 13 tuổi để đăng ký");
                    ModelState.AddModelError("DateOfBirth", "Bạn phải đủ 13 tuổi để đăng ký");
                    _logger.LogWarning("❌ Age validation failed: age {Age} is below minimum", age);
                }

                // If there are validation errors, return the view
                if (validationErrors.Any())
                {
                    _logger.LogWarning("❌ Server-side validation failed for {Email}: {Errors}",
                        model.Email,
                        string.Join(", ", validationErrors.Select(e => $"{e.Key}: {e.Value}")));

                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                _logger.LogInformation("✅ All validations passed, proceeding with registration via AuthService...");

                // Proceed with registration
                var result = await _authService.RegisterCustomerAsync(model);

                if (result.Success)
                {
                    _logger.LogInformation("🎉 New customer registered successfully: {Email}", model.Email);

                    // Set success message for popup
                    TempData["SuccessMessage"] = "Đăng ký thành công! Chào mừng bạn đến với cửa hàng.";
                    TempData["ShowSuccessPopup"] = "true";

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    _logger.LogError("❌ Registration service failed for {Email}: {Error}", model.Email, result.ErrorMessage);
                    _logger.LogDebug("Registration failure details: ValidationErrors={ValidationErrors}, RequiresTwoFactor={RequiresTwoFactor}",
                        string.Join(", ", result.ValidationErrors?.Select(e => $"{e.Key}:{e.Value}") ?? []),
                        result.GetType().GetProperty("RequiresTwoFactor")?.GetValue(result));

                    // Add validation errors from service
                    if (result.ValidationErrors?.Any() == true)
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            ModelState.AddModelError(error.Key, error.Value);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, result.ErrorMessage);
                        // Also set TempData for immediate user feedback
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 CRITICAL: Unexpected error during registration for {Email}. Exception Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                    model.Email ?? "UNKNOWN", ex.GetType().Name, ex.Message, ex.StackTrace);

                // Log inner exceptions if they exist
                var innerEx = ex.InnerException;
                var level = 1;
                while (innerEx != null)
                {
                    _logger.LogError("💥 Inner Exception Level {Level}: Type={Type}, Message={Message}",
                        level, innerEx.GetType().Name, innerEx.Message);
                    innerEx = innerEx.InnerException;
                    level++;
                }

                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại.");
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: /Account/Logout - Perform logout directly
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            // If user is authenticated, sign them out
            if (User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                    await _authService.SignOutAsync();
                    _logger.LogInformation("User {Email} logged out via GET", userEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during GET logout");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutPost()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                await _authService.SignOutAsync();
                _logger.LogInformation("User {Email} logged out via POST", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POST logout");
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var customerId = await _authService.GetCurrentCustomerIdAsync();
                if (!customerId.HasValue)
                {
                    return RedirectToAction("Login");
                }

                var profile = await _customerService.GetCustomerProfileAsync(customerId.Value);
                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer profile");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin tài khoản.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/EditProfile
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            try
            {
                var customerId = await _authService.GetCurrentCustomerIdAsync();
                if (!customerId.HasValue)
                {
                    return RedirectToAction("Login");
                }

                var customer = await _customerService.GetCustomerByIdAsync(customerId.Value);
                if (customer == null)
                {
                    return RedirectToAction("Login");
                }

                var model = new UpdateProfileViewModel
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    PhoneNumber = customer.PhoneNumber,
                    DateOfBirth = customer.DateOfBirth,
                    CurrentImageUrl = customer.ImageUrl
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit profile page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin chỉnh sửa.";
                return RedirectToAction("Profile");
            }
        }

        // POST: /Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditProfile(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var customerId = await _authService.GetCurrentCustomerIdAsync();
                if (!customerId.HasValue)
                {
                    return RedirectToAction("Login");
                }

                var success = await _customerService.UpdateCustomerProfileAsync(customerId.Value, model);

                if (success)
                {
                    // Refresh user claims to update the display name
                    await _authService.RefreshUserClaimsAsync(customerId.Value, "Customer");

                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật thông tin.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer profile");
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật thông tin. Vui lòng thử lại.");
            }

            return View(model);
        }

        // GET: /Account/ChangePassword
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var customerId = await _authService.GetCurrentCustomerIdAsync();
                if (!customerId.HasValue)
                {
                    return RedirectToAction("Login");
                }

                var success = await _authService.ChangePasswordAsync(customerId.Value, "Customer", model);

                if (success)
                {
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại.");
            }

            return View(model);
        }

        // GET: /Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // AJAX: Check if email exists
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmailExists(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { exists = false });
                }

                var exists = await _customerService.EmailExistsAsync(email);
                return Json(new { exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email exists: {Email}", email);
                return Json(new { exists = false });
            }
        }

        // AJAX: Check if phone exists
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CheckPhoneExists(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return Json(new { exists = false });
                }

                var exists = await _customerService.PhoneExistsAsync(phoneNumber);
                return Json(new { exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if phone exists: {PhoneNumber}", phoneNumber);
                return Json(new { exists = false });
            }
        }

        // AJAX: Check if user is authenticated
        [HttpGet]
        [AllowAnonymous]
        public IActionResult IsAuthenticated()
        {
            return Json(new { isAuthenticated = User?.Identity?.IsAuthenticated == true });
        }

        #region Google OAuth

        // GET: /Account/GoogleLogin - Initiate Google OAuth login
        [AllowAnonymous]
        public IActionResult GoogleLogin(string returnUrl = "/")
        {
            // Log request details for debugging OAuth issues
            _logger.LogInformation("🔐 Google OAuth login initiated");
            _logger.LogInformation("   - Request.Scheme: {Scheme}", Request.Scheme);
            _logger.LogInformation("   - Request.Host: {Host}", Request.Host);
            _logger.LogInformation("   - X-Forwarded-Proto: {Proto}", Request.Headers["X-Forwarded-Proto"].FirstOrDefault());
            _logger.LogInformation("   - X-Forwarded-Host: {Host}", Request.Headers["X-Forwarded-Host"].FirstOrDefault());
            
            var redirectUrl = Url.Action("GoogleCallback", "Account", new { returnUrl });
            _logger.LogInformation("   - Generated redirect URL: {RedirectUrl}", redirectUrl);
            
            var properties = new AuthenticationProperties 
            { 
                RedirectUri = redirectUrl,
                Items =
                {
                    { "returnUrl", returnUrl }
                }
            };
            
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET: /Account/GoogleCallback - Handle Google OAuth callback
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback(string returnUrl = "/")
        {
            try
            {
                // Get the external login info from Google
                var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                if (!authenticateResult.Succeeded)
                {
                    _logger.LogWarning("Google authentication failed");
                    TempData["ErrorMessage"] = "Đăng nhập với Google thất bại. Vui lòng thử lại.";
                    return RedirectToAction("Login", new { returnUrl });
                }

                var claims = authenticateResult.Principal?.Claims;
                var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var firstName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "";
                var lastName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "";
                var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var profilePicture = claims?.FirstOrDefault(c => c.Type == "picture" || c.Type == "urn:google:picture")?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Could not get email from Google authentication");
                    TempData["ErrorMessage"] = "Không thể lấy thông tin email từ Google.";
                    return RedirectToAction("Login", new { returnUrl });
                }

                _logger.LogInformation("Google OAuth successful for email: {Email}", email);

                // Check if customer already exists
                var existingCustomer = await _customerService.GetCustomerByEmailAsync(email);

                if (existingCustomer != null)
                {
                    // Customer exists - sign them in
                    await _authService.SignInCustomerAsync(existingCustomer.Id);
                    _logger.LogInformation("Existing customer {Email} logged in via Google", email);
                    
                    TempData["SuccessMessage"] = $"Chào mừng {existingCustomer.FirstName} quay trở lại!";
                }
                else
                {
                    // New customer - create account
                    var registerModel = new RegisterViewModel
                    {
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Password = GenerateSecureRandomPassword(),
                        ConfirmPassword = "",
                        PhoneNumber = "",
                        DateOfBirth = DateTime.Now.AddYears(-25), // Default age
                        AcceptTerms = true
                    };
                    registerModel.ConfirmPassword = registerModel.Password;

                    var result = await _authService.RegisterCustomerWithGoogleAsync(registerModel, googleId, profilePicture);

                    if (result.Success)
                    {
                        _logger.LogInformation("New customer {Email} registered via Google", email);
                        TempData["SuccessMessage"] = "Đăng ký thành công! Chào mừng bạn đến với cửa hàng.";
                        TempData["ShowSuccessPopup"] = "true";
                    }
                    else
                    {
                        _logger.LogError("Failed to register customer via Google: {Error}", result.ErrorMessage);
                        TempData["ErrorMessage"] = result.ErrorMessage ?? "Có lỗi xảy ra khi đăng ký. Vui lòng thử lại.";
                        return RedirectToAction("Login", new { returnUrl });
                    }
                }

                // Clean and validate return URL
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("Admin"))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google OAuth callback");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng nhập với Google. Vui lòng thử lại.";
                return RedirectToAction("Login", new { returnUrl });
            }
        }

        private static string GenerateSecureRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new char[16];
            
            for (int i = 0; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }
            
            return new string(password);
        }

        #endregion

        #region OTP Verification

        // GET: /Account/SendOtp
        [AllowAnonymous]
        public IActionResult SendOtp(string returnUrl = "/", string purpose = "verify")
        {
            if (!_twilioService.IsConfigured)
            {
                TempData["ErrorMessage"] = "Dịch vụ SMS chưa được cấu hình.";
                return RedirectToAction("Login");
            }

            var model = new SendOtpViewModel
            {
                Purpose = purpose,
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        // POST: /Account/SendOtp
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOtp(SendOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check rate limiting - prevent spam
                var lastOtpSent = HttpContext.Session.GetString("LastOtpSentTime");
                if (!string.IsNullOrEmpty(lastOtpSent))
                {
                    var lastSentTime = DateTime.Parse(lastOtpSent);
                    var timeSinceLastSent = DateTime.UtcNow - lastSentTime;
                    if (timeSinceLastSent.TotalSeconds < 60)
                    {
                        var waitTime = 60 - (int)timeSinceLastSent.TotalSeconds;
                        ModelState.AddModelError("", $"Vui lòng đợi {waitTime} giây trước khi gửi lại mã OTP.");
                        return View(model);
                    }
                }

                // Generate OTP
                var otpData = OtpHelper.GenerateOtpWithExpiration(expirationMinutes: 5, length: 6);
                otpData.PhoneNumber = model.PhoneNumber;

                // Send OTP via Twilio
                var result = await _twilioService.SendOtpAsync(model.PhoneNumber, otpData.Code);

                if (result.Success)
                {
                    // Store OTP in session
                    HttpContext.Session.SetString("OtpCode", otpData.Code);
                    HttpContext.Session.SetString("OtpExpiresAt", otpData.ExpiresAt.ToString("O"));
                    HttpContext.Session.SetString("OtpPhoneNumber", model.PhoneNumber);
                    HttpContext.Session.SetString("OtpPurpose", model.Purpose);
                    HttpContext.Session.SetString("LastOtpSentTime", DateTime.UtcNow.ToString("O"));
                    HttpContext.Session.SetInt32("OtpAttemptCount", 0);

                    _logger.LogInformation("OTP sent successfully to {PhoneNumber}", MaskPhoneNumber(model.PhoneNumber));

                    TempData["SuccessMessage"] = "Mã OTP đã được gửi đến số điện thoại của bạn.";

                    return RedirectToAction("VerifyOtp", new { 
                        phoneNumber = model.PhoneNumber, 
                        purpose = model.Purpose, 
                        returnUrl = model.ReturnUrl 
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to send OTP to {PhoneNumber}: {Error}", 
                        MaskPhoneNumber(model.PhoneNumber), result.ErrorMessage);
                    ModelState.AddModelError("", result.ErrorMessage ?? "Không thể gửi mã OTP. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to {PhoneNumber}", MaskPhoneNumber(model.PhoneNumber));
                ModelState.AddModelError("", "Có lỗi xảy ra khi gửi mã OTP. Vui lòng thử lại.");
            }

            return View(model);
        }

        // GET: /Account/VerifyOtp
        [AllowAnonymous]
        public IActionResult VerifyOtp(string phoneNumber, string purpose = "verify", string returnUrl = "/")
        {
            var storedPhone = HttpContext.Session.GetString("OtpPhoneNumber");
            var expiresAtStr = HttpContext.Session.GetString("OtpExpiresAt");

            if (string.IsNullOrEmpty(storedPhone) || string.IsNullOrEmpty(expiresAtStr))
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã OTP. Vui lòng yêu cầu mã mới.";
                return RedirectToAction("SendOtp", new { purpose, returnUrl });
            }

            var expiresAt = DateTime.Parse(expiresAtStr);
            var remainingSeconds = (int)(expiresAt - DateTime.UtcNow).TotalSeconds;

            if (remainingSeconds <= 0)
            {
                TempData["ErrorMessage"] = "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.";
                return RedirectToAction("SendOtp", new { purpose, returnUrl });
            }

            var model = new VerifyOtpViewModel
            {
                PhoneNumber = storedPhone,
                Purpose = purpose,
                ReturnUrl = returnUrl,
                RemainingSeconds = remainingSeconds,
                CanResend = true
            };

            return View(model);
        }

        // POST: /Account/VerifyOtp
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get stored OTP data from session
                var storedCode = HttpContext.Session.GetString("OtpCode");
                var storedPhone = HttpContext.Session.GetString("OtpPhoneNumber");
                var expiresAtStr = HttpContext.Session.GetString("OtpExpiresAt");
                var attemptCount = HttpContext.Session.GetInt32("OtpAttemptCount") ?? 0;

                // Validate stored data
                if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(storedPhone) || string.IsNullOrEmpty(expiresAtStr))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mã OTP. Vui lòng yêu cầu mã mới.";
                    return RedirectToAction("SendOtp", new { purpose = model.Purpose, returnUrl = model.ReturnUrl });
                }

                // Check attempt count
                if (attemptCount >= 3)
                {
                    // Clear OTP data after max attempts
                    ClearOtpSession();
                    TempData["ErrorMessage"] = "Bạn đã nhập sai quá 3 lần. Vui lòng yêu cầu mã OTP mới.";
                    return RedirectToAction("SendOtp", new { purpose = model.Purpose, returnUrl = model.ReturnUrl });
                }

                // Create OTP data for validation
                var otpData = new OtpData
                {
                    Code = storedCode,
                    PhoneNumber = storedPhone,
                    ExpiresAt = DateTime.Parse(expiresAtStr),
                    AttemptCount = attemptCount
                };

                // Validate OTP
                var validationResult = OtpHelper.ValidateOtp(otpData, model.OtpCode);

                if (!validationResult.IsValid)
                {
                    // Increment attempt count
                    HttpContext.Session.SetInt32("OtpAttemptCount", attemptCount + 1);
                    
                    _logger.LogWarning("Invalid OTP attempt {AttemptCount} for {PhoneNumber}", 
                        attemptCount + 1, MaskPhoneNumber(storedPhone));

                    ModelState.AddModelError("OtpCode", validationResult.ErrorMessage ?? "Mã OTP không chính xác.");
                    
                    // Recalculate remaining time
                    model.RemainingSeconds = OtpHelper.GetRemainingSeconds(otpData);
                    return View(model);
                }

                // OTP verified successfully
                _logger.LogInformation("OTP verified successfully for {PhoneNumber}", MaskPhoneNumber(storedPhone));

                // Clear OTP session data
                ClearOtpSession();

                // Mark phone as verified in session
                HttpContext.Session.SetString("VerifiedPhoneNumber", storedPhone);
                HttpContext.Session.SetString("PhoneVerifiedAt", DateTime.UtcNow.ToString("O"));

                TempData["SuccessMessage"] = "Xác thực số điện thoại thành công!";

                // Handle different purposes
                switch (model.Purpose.ToLower())
                {
                    case "register":
                        return RedirectToAction("Register", new { verifiedPhone = storedPhone });
                    
                    case "login":
                        // For phone login flow
                        return RedirectToAction("PhoneLogin", new { phoneNumber = storedPhone });
                    
                    case "reset_password":
                        return RedirectToAction("ResetPassword", new { phoneNumber = storedPhone });
                    
                    default:
                        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return LocalRedirect(model.ReturnUrl);
                        }
                        return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP");
                ModelState.AddModelError("", "Có lỗi xảy ra khi xác thực mã OTP. Vui lòng thử lại.");
            }

            return View(model);
        }

        // POST: /Account/ResendOtp
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string purpose = "verify", string returnUrl = "/")
        {
            var storedPhone = HttpContext.Session.GetString("OtpPhoneNumber");

            if (string.IsNullOrEmpty(storedPhone))
            {
                TempData["ErrorMessage"] = "Không tìm thấy số điện thoại. Vui lòng thử lại.";
                return RedirectToAction("SendOtp", new { purpose, returnUrl });
            }

            // Check rate limiting
            var lastOtpSent = HttpContext.Session.GetString("LastOtpSentTime");
            if (!string.IsNullOrEmpty(lastOtpSent))
            {
                var lastSentTime = DateTime.Parse(lastOtpSent);
                var timeSinceLastSent = DateTime.UtcNow - lastSentTime;
                if (timeSinceLastSent.TotalSeconds < 60)
                {
                    var waitTime = 60 - (int)timeSinceLastSent.TotalSeconds;
                    TempData["ErrorMessage"] = $"Vui lòng đợi {waitTime} giây trước khi gửi lại mã OTP.";
                    return RedirectToAction("VerifyOtp", new { phoneNumber = storedPhone, purpose, returnUrl });
                }
            }

            try
            {
                // Generate new OTP
                var otpData = OtpHelper.GenerateOtpWithExpiration(expirationMinutes: 5, length: 6);

                // Send OTP via Twilio
                var result = await _twilioService.SendOtpAsync(storedPhone, otpData.Code);

                if (result.Success)
                {
                    // Update session with new OTP
                    HttpContext.Session.SetString("OtpCode", otpData.Code);
                    HttpContext.Session.SetString("OtpExpiresAt", otpData.ExpiresAt.ToString("O"));
                    HttpContext.Session.SetString("LastOtpSentTime", DateTime.UtcNow.ToString("O"));
                    HttpContext.Session.SetInt32("OtpAttemptCount", 0);

                    _logger.LogInformation("OTP resent successfully to {PhoneNumber}", MaskPhoneNumber(storedPhone));
                    TempData["SuccessMessage"] = "Mã OTP mới đã được gửi đến số điện thoại của bạn.";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Không thể gửi lại mã OTP. Vui lòng thử lại sau.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP to {PhoneNumber}", MaskPhoneNumber(storedPhone));
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi lại mã OTP.";
            }

            return RedirectToAction("VerifyOtp", new { phoneNumber = storedPhone, purpose, returnUrl });
        }

        // AJAX: Check OTP status
        [HttpGet]
        [AllowAnonymous]
        public IActionResult CheckOtpStatus()
        {
            var expiresAtStr = HttpContext.Session.GetString("OtpExpiresAt");
            var attemptCount = HttpContext.Session.GetInt32("OtpAttemptCount") ?? 0;
            var lastSentStr = HttpContext.Session.GetString("LastOtpSentTime");

            if (string.IsNullOrEmpty(expiresAtStr))
            {
                return Json(new { expired = true, remainingSeconds = 0, canResend = true });
            }

            var expiresAt = DateTime.Parse(expiresAtStr);
            var remainingSeconds = (int)(expiresAt - DateTime.UtcNow).TotalSeconds;
            
            var canResend = true;
            var resendWaitSeconds = 0;
            if (!string.IsNullOrEmpty(lastSentStr))
            {
                var timeSinceLastSent = DateTime.UtcNow - DateTime.Parse(lastSentStr);
                if (timeSinceLastSent.TotalSeconds < 60)
                {
                    canResend = false;
                    resendWaitSeconds = 60 - (int)timeSinceLastSent.TotalSeconds;
                }
            }

            return Json(new
            {
                expired = remainingSeconds <= 0,
                remainingSeconds = Math.Max(0, remainingSeconds),
                attemptCount = attemptCount,
                maxAttempts = 3,
                canResend = canResend,
                resendWaitSeconds = resendWaitSeconds
            });
        }

        private void ClearOtpSession()
        {
            HttpContext.Session.Remove("OtpCode");
            HttpContext.Session.Remove("OtpExpiresAt");
            HttpContext.Session.Remove("OtpPhoneNumber");
            HttpContext.Session.Remove("OtpPurpose");
            HttpContext.Session.Remove("OtpAttemptCount");
        }

        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 8)
                return "***";
            return phoneNumber.Substring(0, 4) + "****" + phoneNumber.Substring(phoneNumber.Length - 3);
        }

        #endregion

        // Debug endpoint - remove in production
        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestRegistration()
        {
            var model = new RegisterViewModel
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                PhoneNumber = "0901234567",
                DateOfBirth = DateTime.Now.AddYears(-25),
                AcceptTerms = true
            };

            return View("Register", model);
        }

        // Debug endpoint to check model binding
        [HttpPost]
        [AllowAnonymous]
        public IActionResult DebugModel(RegisterViewModel model)
        {
            var debugInfo = new
            {
                IsValid = ModelState.IsValid,
                Errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new {
                        Field = x.Key,
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                    }),
                Model = model
            };

            return Json(debugInfo);
        }

        // DEBUG: Enhanced debug endpoint to test registration with detailed logging
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DebugRegistration([FromBody] RegisterViewModel model)
        {
            var debugInfo = new Dictionary<string, object>();

            try
            {
                _logger.LogInformation("🔧 DEBUG: Starting debug registration test");

                debugInfo["step"] = "1. Model Validation";
                debugInfo["modelState"] = new
                {
                    IsValid = ModelState.IsValid,
                    Errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new {
                            Field = x.Key,
                            Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                        }),
                    Model = new
                    {
                        model.FirstName,
                        model.LastName,
                        model.Email,
                        model.PhoneNumber,
                        model.DateOfBirth,
                        model.AcceptTerms,
                        PasswordLength = model.Password?.Length ?? 0
                    }
                };

                if (ModelState.IsValid)
                {
                    debugInfo["step"] = "2. Service Validation";

                    // Test email exists
                    var emailExists = await _customerService.EmailExistsAsync(model.Email);
                    debugInfo["emailExists"] = emailExists;

                    // Test phone exists  
                    var phoneExists = await _customerService.PhoneExistsAsync(model.PhoneNumber);
                    debugInfo["phoneExists"] = phoneExists;

                    if (!emailExists && !phoneExists)
                    {
                        debugInfo["step"] = "3. Registration Attempt";

                        // Attempt registration
                        var result = await _authService.RegisterCustomerAsync(model);
                        debugInfo["registrationResult"] = new
                        {
                            result.Success,
                            result.ErrorMessage,
                            ValidationErrors = result.ValidationErrors,
                            CustomerCreated = result.Customer != null,
                            CustomerId = result.Customer?.Id
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔧 DEBUG: Exception in debug registration");
                debugInfo["exception"] = new
                {
                    Type = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException?.Message
                };
            }

            return Json(debugInfo);
        }

        //// Enhanced debug endpoint to test all validation scenarios
        //[HttpPost]
        //[AllowAnonymous]
        //public async Task<IActionResult> DebugRegistrationDetailed([FromBody] RegisterViewModel model)
        //{
        //    var debugInfo = new Dictionary<string, object>();

        //    try
        //    {
        //        _logger.LogInformation("🔧 DEBUG: Starting comprehensive registration test for {Email}", model.Email);

        //        debugInfo["step"] = "1. Input Analysis";
        //        debugInfo["inputData"] = new
        //        {
        //            FirstName = model.FirstName,
        //            LastName = model.LastName,
        //            Email = model.Email,
        //            PhoneNumber = model.PhoneNumber,
        //            DateOfBirth = model.DateOfBirth,
        //            DateOfBirthString = model.DateOfBirth.ToString("yyyy-MM-dd HH:mm:ss"),
        //            DateOfBirthKind = model.DateOfBirth.Kind,
        //            DateOfBirthYear = model.DateOfBirth.Year,
        //            AcceptTerms = model.AcceptTerms,
        //            PasswordLength = model.Password?.Length ?? 0,
        //            IsMinValue = model.DateOfBirth == DateTime.MinValue,
        //            IsValidYear = model.DateOfBirth.Year >= 1900 && model.DateOfBirth.Year <= DateTime.Now.Year
        //        };

        //        debugInfo["step"] = "2. Model State Validation";
        //        debugInfo["modelState"] = new
        //        {
        //            IsValid = ModelState.IsValid,
        //            Errors = ModelState
        //                .Where(x => x.Value.Errors.Count > 0)
        //                .Select(x => new
        //                {
        //                    Field = x.Key,
        //                    Errors = x.Value.Errors.Select(e => e.ErrorMessage)
        //                })
        //        };

        //        if (ModelState.IsValid)
        //        {
        //            debugInfo["step"] = "3. Service Validation";

        //            // Test email exists
        //            var emailExists = await _customerService.EmailExistsAsync(model.Email);
        //            debugInfo["emailExists"] = emailExists;

        //            // Test phone exists  
        //            var phoneExists = await _customerService.PhoneExistsAsync(model.PhoneNumber);
        //            debugInfo["phoneExists"] = phoneExists;

        //            // Test data validation
        //            var dataValid = await _customerService.ValidateRegistrationDataAsync(model);
        //            debugInfo["dataValidationResult"] = dataValid;

        //            if (!emailExists && !phoneExists && dataValid)
        //            {
        //                debugInfo["step"] = "4. Customer Service Registration";

        //                // Attempt registration via CustomerService directly
        //                var serviceResult = await _customerService.RegisterCustomerAsync(model);
        //                debugInfo["customerServiceResult"] = new
        //                {
        //                    serviceResult.Success,
        //                    serviceResult.ErrorMessage,
        //                    ValidationErrors = serviceResult.ValidationErrors,
        //                    CustomerCreated = serviceResult.Customer != null,
        //                    CustomerId = serviceResult.Customer?.Id,
        //                    CustomerEmail = serviceResult.Customer?.Email,
        //                    CustomerPhone = serviceResult.Customer?.PhoneNumber,
        //                    CustomerDOB = serviceResult.Customer?.DateOfBirth.ToString("yyyy-MM-dd")
        //                };

        //                if (serviceResult.Success)
        //                {
        //                    debugInfo["step"] = "5. Auth Service Registration Test";

        //                    // Also test the full AuthService flow
        //                    var authResult = await _authService.RegisterCustomerAsync(model);
        //                    debugInfo["authServiceResult"] = new
        //                    {
        //                        authResult.Success,
        //                        authResult.ErrorMessage,
        //                        ValidationErrors = authResult.ValidationErrors,
        //                        CustomerCreated = authResult.Customer != null,
        //                        CustomerId = authResult.Customer?.Id
        //                    };
        //                }
        //            }
        //            else
        //            {
        //                debugInfo["validationFailures"] = new
        //                {
        //                    EmailExists = emailExists,
        //                    PhoneExists = phoneExists,
        //                    DataValid = dataValid
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "🔧 DEBUG: Exception in comprehensive debug registration");
        //        debugInfo["exception"] = new
        //        {
        //            Type = ex.GetType().Name,
        //            Message = ex.Message,
        //            StackTrace = ex.StackTrace?.Split('\n').Take(10), // Limit stack trace
        //            InnerException = ex.InnerException?.Message,
        //            Data = ex.Data?.Count > 0 ? ex.Data : null
        //        };
        //    }

        //    return Json(debugInfo);
        //}
    }
}