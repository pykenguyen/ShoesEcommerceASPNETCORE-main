namespace ShoesEcommerce.Services.Options
{
    /// <summary>
    /// Configuration options for Social Media Chat integrations (Facebook, Zalo)
    /// </summary>
    public class SocialChatOptions
    {
        /// <summary>
        /// The configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "SocialChat";

        /// <summary>
        /// Facebook Messenger Click-to-Chat configuration (2025 standard)
        /// Note: Customer Chat Plugin deprecated by Meta, using m.me links instead
        /// </summary>
        public FacebookMessengerOptions Facebook { get; set; } = new();

        /// <summary>
        /// Zalo OA Chat configuration
        /// </summary>
        public ZaloChatOptions Zalo { get; set; } = new();
    }

    /// <summary>
    /// Facebook Messenger options (Click-to-Messenger - 2025 standard)
    /// Meta deprecated Customer Chat Plugin, use m.me links instead
    /// </summary>
    public class FacebookMessengerOptions
    {
        /// <summary>
        /// Whether Facebook Messenger button is enabled
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Facebook Page ID or Page Username for m.me link
        /// Example: "937477202779723" or "YourPageName"
        /// </summary>
        public string PageId { get; set; } = string.Empty;

        /// <summary>
        /// Facebook App ID (optional, for analytics)
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Theme color for the chat button (hex color without #)
        /// </summary>
        public string ThemeColor { get; set; } = "0084FF";

        /// <summary>
        /// Button text displayed
        /// </summary>
        public string ButtonText { get; set; } = "Chat Facebook";

        /// <summary>
        /// Default ref parameter for tracking (optional)
        /// </summary>
        public string DefaultRef { get; set; } = "website_chat";
    }

    /// <summary>
    /// Zalo OA Chat options
    /// </summary>
    public class ZaloChatOptions
    {
        /// <summary>
        /// Whether Zalo chat is enabled
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Zalo OA ID (Official Account ID)
        /// </summary>
        public string OaId { get; set; } = string.Empty;

        /// <summary>
        /// Welcome message when chat opens
        /// </summary>
        public string WelcomeMessage { get; set; } = "Xin chào! SPORTS Vietnam có thể giúp gì cho bạn?";

        /// <summary>
        /// Auto popup delay in seconds (0 = disabled)
        /// </summary>
        public int AutoPopupDelay { get; set; } = 0;
    }
}
