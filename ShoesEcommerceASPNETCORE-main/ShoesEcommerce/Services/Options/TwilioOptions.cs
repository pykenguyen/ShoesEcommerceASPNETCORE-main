namespace ShoesEcommerce.Services.Options
{
    /// <summary>
    /// Configuration options for Twilio SMS service
    /// </summary>
    public class TwilioOptions
    {
        public const string SectionName = "Twilio";

        /// <summary>
        /// Twilio Account SID
        /// </summary>
        public string AccountSid { get; set; } = string.Empty;

        /// <summary>
        /// Twilio Auth Token
        /// </summary>
        public string AuthToken { get; set; } = string.Empty;

        /// <summary>
        /// Twilio Phone Number for sending SMS
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// OTP code length (default: 6 digits)
        /// </summary>
        public int OtpLength { get; set; } = 6;

        /// <summary>
        /// OTP expiration time in minutes (default: 5 minutes)
        /// </summary>
        public int OtpExpirationMinutes { get; set; } = 5;
    }
}
