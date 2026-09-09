using Microsoft.Extensions.Options;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Twilio.Exceptions;

namespace ShoesEcommerce.Services
{
    /// <summary>
    /// Service for sending OTP via Twilio SMS
    /// </summary>
    public class TwilioService : ITwilioService
    {
        private readonly TwilioOptions _options;
        private readonly ILogger<TwilioService> _logger;
        private readonly bool _isConfigured;

        public TwilioService(
            IOptions<TwilioOptions> options,
            ILogger<TwilioService> logger)
        {
            _options = options.Value;
            _logger = logger;

            // Check if Twilio is configured
            _isConfigured = !string.IsNullOrEmpty(_options.AccountSid) 
                && !string.IsNullOrEmpty(_options.AuthToken)
                && !string.IsNullOrEmpty(_options.PhoneNumber);

            if (_isConfigured)
            {
                TwilioClient.Init(_options.AccountSid, _options.AuthToken);
                _logger.LogInformation("? Twilio Service initialized successfully");
            }
            else
            {
                _logger.LogWarning("?? Twilio Service not configured. Set Twilio:AccountSid, Twilio:AuthToken, and Twilio:PhoneNumber in configuration.");
            }
        }

        /// <summary>
        /// Send OTP code via SMS
        /// </summary>
        /// <param name="toPhoneNumber">Recipient phone number (E.164 format)</param>
        /// <param name="otp">OTP code to send</param>
        /// <returns>True if SMS was sent successfully</returns>
        public async Task<OtpResult> SendOtpAsync(string toPhoneNumber, string otp)
        {
            if (!_isConfigured)
            {
                _logger.LogError("? Twilio is not configured. Cannot send OTP.");
                return OtpResult.Failure("D?ch v? SMS ch?a ???c c?u hình.");
            }

            if (string.IsNullOrWhiteSpace(toPhoneNumber))
            {
                return OtpResult.Failure("S? ?i?n tho?i không h?p l?.");
            }

            if (string.IsNullOrWhiteSpace(otp))
            {
                return OtpResult.Failure("Mã OTP không h?p l?.");
            }

            try
            {
                // Format phone number to E.164 if needed
                var formattedPhone = FormatPhoneNumber(toPhoneNumber);

                var message = await MessageResource.CreateAsync(
                    body: $"Mã xác th?c c?a b?n là: {otp}. Mã có hi?u l?c trong {_options.OtpExpirationMinutes} phút.",
                    from: new PhoneNumber(_options.PhoneNumber),
                    to: new PhoneNumber(formattedPhone)
                );

                _logger.LogInformation("? OTP sent successfully to {PhoneNumber}. Message SID: {MessageSid}", 
                    MaskPhoneNumber(formattedPhone), message.Sid);

                return OtpResult.SuccessResult(message.Sid);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "? Twilio API error sending OTP to {PhoneNumber}. Error Code: {ErrorCode}", 
                    MaskPhoneNumber(toPhoneNumber), ex.Code);
                
                return ex.Code switch
                {
                    21211 => OtpResult.Failure("S? ?i?n tho?i không h?p l?."),
                    21614 => OtpResult.Failure("S? ?i?n tho?i không th? nh?n SMS."),
                    21608 => OtpResult.Failure("S? ?i?n tho?i ch?a ???c xác minh trong Twilio trial."),
                    _ => OtpResult.Failure($"Không th? g?i SMS. Vui lòng th? l?i sau. (Mã l?i: {ex.Code})")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Unexpected error sending OTP to {PhoneNumber}", 
                    MaskPhoneNumber(toPhoneNumber));
                return OtpResult.Failure("Có l?i x?y ra khi g?i mã OTP. Vui lòng th? l?i.");
            }
        }

        /// <summary>
        /// Send custom message via SMS
        /// </summary>
        public async Task<OtpResult> SendSmsAsync(string toPhoneNumber, string message)
        {
            if (!_isConfigured)
            {
                _logger.LogError("? Twilio is not configured. Cannot send SMS.");
                return OtpResult.Failure("D?ch v? SMS ch?a ???c c?u hình.");
            }

            try
            {
                var formattedPhone = FormatPhoneNumber(toPhoneNumber);

                var smsMessage = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_options.PhoneNumber),
                    to: new PhoneNumber(formattedPhone)
                );

                _logger.LogInformation("? SMS sent successfully to {PhoneNumber}. Message SID: {MessageSid}", 
                    MaskPhoneNumber(formattedPhone), smsMessage.Sid);

                return OtpResult.SuccessResult(smsMessage.Sid);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "? Twilio API error sending SMS to {PhoneNumber}", 
                    MaskPhoneNumber(toPhoneNumber));
                return OtpResult.Failure($"Không th? g?i SMS. (Mã l?i: {ex.Code})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Unexpected error sending SMS to {PhoneNumber}", 
                    MaskPhoneNumber(toPhoneNumber));
                return OtpResult.Failure("Có l?i x?y ra khi g?i SMS. Vui lòng th? l?i.");
            }
        }

        /// <summary>
        /// Check if Twilio service is configured
        /// </summary>
        public bool IsConfigured => _isConfigured;

        /// <summary>
        /// Format Vietnamese phone number to E.164 format
        /// </summary>
        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return phoneNumber;

            // Remove all non-digit characters except +
            var cleaned = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // If already in E.164 format (starts with +)
            if (cleaned.StartsWith("+"))
                return cleaned;

            // Vietnamese phone number conversion
            if (cleaned.StartsWith("0"))
            {
                // Remove leading 0 and add +84
                return "+84" + cleaned.Substring(1);
            }

            // If starts with 84, add +
            if (cleaned.StartsWith("84"))
            {
                return "+" + cleaned;
            }

            // Default: add + prefix
            return "+" + cleaned;
        }

        /// <summary>
        /// Mask phone number for logging (hide middle digits)
        /// </summary>
        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 8)
                return "***";

            return phoneNumber.Substring(0, 4) + "****" + phoneNumber.Substring(phoneNumber.Length - 3);
        }
    }

    /// <summary>
    /// Result of OTP/SMS operation
    /// </summary>
    public class OtpResult
    {
        public bool Success { get; set; }
        public string? MessageSid { get; set; }
        public string? ErrorMessage { get; set; }

        public static OtpResult SuccessResult(string messageSid)
        {
            return new OtpResult { Success = true, MessageSid = messageSid };
        }

        public static OtpResult Failure(string errorMessage)
        {
            return new OtpResult { Success = false, ErrorMessage = errorMessage };
        }
    }
}
