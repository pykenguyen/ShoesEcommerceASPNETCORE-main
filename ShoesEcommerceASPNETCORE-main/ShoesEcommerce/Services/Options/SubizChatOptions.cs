namespace ShoesEcommerce.Services.Options
{
    /// <summary>
    /// Configuration options for Subiz Live Chat integration
    /// </summary>
    public class SubizChatOptions
    {
        /// <summary>
        /// The configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "SubizChat";

        /// <summary>
        /// Subiz account ID for the chat widget
        /// </summary>
        public string AccountId { get; set; } = string.Empty;

        /// <summary>
        /// Whether the chat widget is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
