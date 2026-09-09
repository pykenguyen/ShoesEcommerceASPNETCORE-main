using System.ComponentModel.DataAnnotations;

namespace ShoesEcommerce.ViewModels.Account
{
    /// <summary>
    /// ViewModel for sending OTP
    /// </summary>
    public class SendOtpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nh?p s? ?i?n tho?i")]
        [Phone(ErrorMessage = "S? ?i?n tho?i không h?p l?")]
        [RegularExpression(@"^(\+84|84|0)?[3|5|7|8|9]\d{8}$", ErrorMessage = "S? ?i?n tho?i Vi?t Nam không h?p l?")]
        [Display(Name = "S? ?i?n tho?i")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Purpose of OTP (e.g., "register", "login", "reset_password")
        /// </summary>
        public string Purpose { get; set; } = "verify";

        /// <summary>
        /// Return URL after successful verification
        /// </summary>
        public string? ReturnUrl { get; set; }
    }

    /// <summary>
    /// ViewModel for verifying OTP
    /// </summary>
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nh?p mã OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP ph?i có 6 ch? s?")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP ch? ch?a 6 ch? s?")]
        [Display(Name = "Mã OTP")]
        public string OtpCode { get; set; } = string.Empty;

        /// <summary>
        /// Phone number that received the OTP
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Purpose of OTP verification
        /// </summary>
        public string Purpose { get; set; } = "verify";

        /// <summary>
        /// Return URL after successful verification
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Remaining seconds until OTP expires
        /// </summary>
        public int RemainingSeconds { get; set; }

        /// <summary>
        /// Whether user can resend OTP
        /// </summary>
        public bool CanResend { get; set; } = true;
    }

    /// <summary>
    /// Result of OTP verification
    /// </summary>
    public class OtpVerificationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? RedirectUrl { get; set; }

        public static OtpVerificationResult SuccessResult(string message, string? redirectUrl = null)
        {
            return new OtpVerificationResult
            {
                Success = true,
                Message = message,
                RedirectUrl = redirectUrl
            };
        }

        public static OtpVerificationResult FailedResult(string message)
        {
            return new OtpVerificationResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
