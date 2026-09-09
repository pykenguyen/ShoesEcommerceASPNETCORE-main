namespace ShoesEcommerce.Services.Options
{
    /// <summary>
    /// Email configuration settings - Supports SMTP and Mailchimp
    /// </summary>
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";
        
        /// <summary>
        /// Email provider: "SMTP" or "Mailchimp"
        /// </summary>
        public string Provider { get; set; } = "Mailchimp";
        
        // ==================== Mailchimp Settings ====================
        
        /// <summary>
        /// Mailchimp Marketing API Key
        /// Get from: Mailchimp > Profile > Extras > API keys
        /// Format: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx-usXX
        /// </summary>
        public string MailchimpApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Mailchimp Server Prefix (e.g., "us10" extracted from API key)
        /// The last part after the dash in API key
        /// </summary>
        public string MailchimpServerPrefix { get; set; } = string.Empty;
        
        /// <summary>
        /// Mailchimp Audience/List ID for subscribers
        /// Get from: Audience > Settings > Audience name and defaults
        /// </summary>
        public string MailchimpAudienceId { get; set; } = string.Empty;
        
        /// <summary>
        /// Mailchimp User ID for embed forms
        /// Get from: Mailchimp embed form code (u= parameter)
        /// </summary>
        public string MailchimpUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Mailchimp Template IDs for different email types
        /// </summary>
        public MailchimpTemplates Templates { get; set; } = new();
        
        // ==================== SMTP Settings (Fallback) ====================
        
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool UseStartTls { get; set; } = true;
        
        // ==================== Common Settings ====================
        
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "SPORTS Vietnam";
        public string WebsiteUrl { get; set; } = "https://localhost:7085";
        public string LogoUrl { get; set; } = "/images/logo.png";
        public bool Enabled { get; set; } = true;
    }
    
    /// <summary>
    /// Mailchimp template IDs for different email types
    /// </summary>
    public class MailchimpTemplates
    {
        public int OrderConfirmation { get; set; }
        public int OrderProcessing { get; set; }
        public int OrderShipped { get; set; }
        public int OrderDelivered { get; set; }
        public int OrderCancelled { get; set; }
        public int WelcomeEmail { get; set; }
        public int PasswordReset { get; set; }
        public int AbandonedCart { get; set; }
        public int PromotionCampaign { get; set; }
    }
}
