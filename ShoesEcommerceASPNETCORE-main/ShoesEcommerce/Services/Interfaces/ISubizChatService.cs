namespace ShoesEcommerce.Services.Interfaces
{
    /// <summary>
    /// Interface for Subiz live chat service
    /// </summary>
    public interface ISubizChatService
    {
        /// <summary>
        /// Gets the Subiz account ID
        /// </summary>
        string AccountId { get; }

        /// <summary>
        /// Gets whether the chat widget is enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Generates the JavaScript initialization script for Subiz chat widget
        /// </summary>
        /// <returns>The JavaScript code to initialize Subiz chat</returns>
        string GetInitScript();

        /// <summary>
        /// Generates the JavaScript code to set user attributes
        /// </summary>
        /// <param name="name">User's full name</param>
        /// <param name="email">User's email address</param>
        /// <param name="additionalAttributes">Additional custom attributes</param>
        /// <returns>The JavaScript code to set user attributes</returns>
        string GetSetUserAttributesScript(string? name, string? email, Dictionary<string, string>? additionalAttributes = null);
    }
}
