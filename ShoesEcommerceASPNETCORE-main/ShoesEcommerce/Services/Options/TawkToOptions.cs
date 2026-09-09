namespace ShoesEcommerce.Services.Options
{
    /// <summary>
    /// Configuration options for Tawk.to Live Chat integration
    /// Free live chat solution - https://www.tawk.to/
    /// </summary>
    public class TawkToOptions
    {
        /// <summary>
        /// The configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "TawkTo";

        /// <summary>
        /// Whether Tawk.to chat is enabled
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Tawk.to Property ID (Site ID)
        /// Found in Tawk.to Dashboard -> Administration -> Channels -> Chat Widget
        /// Example: 507f1f77bcf86cd799439011
        /// </summary>
        public string PropertyId { get; set; } = string.Empty;

        /// <summary>
        /// Tawk.to Widget ID
        /// Found in the widget installation code as the second parameter
        /// Example: 1h0ht8eqj
        /// </summary>
        public string WidgetId { get; set; } = string.Empty;

        /// <summary>
        /// Direct embed URL (optional - auto-generated from PropertyId and WidgetId)
        /// Format: https://embed.tawk.to/{PropertyId}/{WidgetId}
        /// </summary>
        public string EmbedUrl => !string.IsNullOrEmpty(PropertyId) && !string.IsNullOrEmpty(WidgetId)
            ? $"https://embed.tawk.to/{PropertyId}/{WidgetId}"
            : string.Empty;
    }
}
