namespace ShoesEcommerce.Services.Interfaces
{
    /// <summary>
    /// Interface for Twilio SMS service
    /// </summary>
    public interface ITwilioService
    {
        /// <summary>
        /// Send OTP code via SMS
        /// </summary>
        /// <param name="toPhoneNumber">Recipient phone number</param>
        /// <param name="otp">OTP code to send</param>
        /// <returns>Result of the operation</returns>
        Task<OtpResult> SendOtpAsync(string toPhoneNumber, string otp);

        /// <summary>
        /// Send custom message via SMS
        /// </summary>
        /// <param name="toPhoneNumber">Recipient phone number</param>
        /// <param name="message">Message content</param>
        /// <returns>Result of the operation</returns>
        Task<OtpResult> SendSmsAsync(string toPhoneNumber, string message);

        /// <summary>
        /// Check if Twilio service is configured
        /// </summary>
        bool IsConfigured { get; }
    }
}
