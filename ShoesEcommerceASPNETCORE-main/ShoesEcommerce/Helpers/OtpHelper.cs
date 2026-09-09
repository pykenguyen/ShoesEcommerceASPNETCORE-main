using System.Security.Cryptography;

namespace ShoesEcommerce.Helpers
{
    /// <summary>
    /// Helper class for OTP generation and management
    /// </summary>
    public static class OtpHelper
    {
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        /// <summary>
        /// Generate a cryptographically secure random OTP code
        /// </summary>
        /// <param name="length">Length of OTP (default: 6 digits)</param>
        /// <returns>OTP code as string</returns>
        public static string GenerateOtp(int length = 6)
        {
            if (length < 4 || length > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "OTP length must be between 4 and 10 digits.");
            }

            var bytes = new byte[length];
            _rng.GetBytes(bytes);

            var otp = new char[length];
            for (int i = 0; i < length; i++)
            {
                otp[i] = (char)('0' + (bytes[i] % 10));
            }

            return new string(otp);
        }

        /// <summary>
        /// Generate OTP with expiration time
        /// </summary>
        /// <param name="expirationMinutes">Expiration time in minutes</param>
        /// <param name="length">Length of OTP</param>
        /// <returns>OTP data with code and expiration</returns>
        public static OtpData GenerateOtpWithExpiration(int expirationMinutes = 5, int length = 6)
        {
            return new OtpData
            {
                Code = GenerateOtp(length),
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Validate if OTP is still valid (not expired)
        /// </summary>
        /// <param name="otpData">OTP data to validate</param>
        /// <param name="inputCode">Code entered by user</param>
        /// <returns>Validation result</returns>
        public static OtpValidationResult ValidateOtp(OtpData? otpData, string inputCode)
        {
            if (otpData == null)
            {
                return OtpValidationResult.Failed("Không tìm th?y mã OTP. Vui lòng yêu c?u mã m?i.");
            }

            if (string.IsNullOrWhiteSpace(inputCode))
            {
                return OtpValidationResult.Failed("Vui lòng nh?p mã OTP.");
            }

            if (DateTime.UtcNow > otpData.ExpiresAt)
            {
                return OtpValidationResult.Failed("Mã OTP ?ã h?t h?n. Vui lòng yêu c?u mã m?i.");
            }

            // Case-insensitive comparison for numeric OTP
            if (!string.Equals(otpData.Code, inputCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return OtpValidationResult.Failed("Mã OTP không chính xác. Vui lòng ki?m tra l?i.");
            }

            return OtpValidationResult.Success();
        }

        /// <summary>
        /// Get remaining seconds until OTP expires
        /// </summary>
        public static int GetRemainingSeconds(OtpData? otpData)
        {
            if (otpData == null)
                return 0;

            var remaining = (otpData.ExpiresAt - DateTime.UtcNow).TotalSeconds;
            return remaining > 0 ? (int)remaining : 0;
        }
    }

    /// <summary>
    /// OTP data container
    /// </summary>
    public class OtpData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? PhoneNumber { get; set; }
        public int AttemptCount { get; set; }
        public const int MaxAttempts = 3;

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool HasMaxAttempts => AttemptCount >= MaxAttempts;
    }

    /// <summary>
    /// OTP validation result
    /// </summary>
    public class OtpValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        public static OtpValidationResult Success()
        {
            return new OtpValidationResult { IsValid = true };
        }

        public static OtpValidationResult Failed(string errorMessage)
        {
            return new OtpValidationResult { IsValid = false, ErrorMessage = errorMessage };
        }
    }
}
