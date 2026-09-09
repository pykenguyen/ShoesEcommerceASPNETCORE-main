using Microsoft.Extensions.Options;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services.Options;
using System.Text;
using System.Text.Json;

namespace ShoesEcommerce.Services
{
    /// <summary>
    /// Service for managing Subiz live chat integration
    /// </summary>
    public class SubizChatService : ISubizChatService
    {
        private readonly SubizChatOptions _options;

        public SubizChatService(IOptions<SubizChatOptions> options)
        {
            _options = options.Value;
        }

        /// <inheritdoc />
        public string AccountId => _options.AccountId;

        /// <inheritdoc />
        public bool IsEnabled => _options.Enabled && !string.IsNullOrEmpty(_options.AccountId);

        /// <inheritdoc />
        public string GetInitScript()
        {
            if (!IsEnabled)
                return string.Empty;

            return $@"
!function(s,u,b,i,z){{
    var o,t,r,y;
    s[i]||(s._sbzaccid=z,s[i]=function(){{s[i].q.push(arguments)}},s[i].q=[],
    s[i](""setAccount"",z),
    r=[""widget.subiz.net"",""storage.googleapis""+(t="".com""),""app.sbz.workers.dev"",i+""a""+(o=function(k,t){{var n=t<=6?5:o(k,t-1)+o(k,t-3);return k!==t?n:n.toString(32)}})(20,20)+t,i+""b""+o(30,30)+t,i+""c""+o(40,40)+t],
    (y=function(k){{var t,n;s._subiz_init_2094850928430||r[k]&&(t=u.createElement(b),n=u.getElementsByTagName(b)[0],t.async=1,t.src=""https://""+r[k]+""/sbz/app.js?accid=""+z,n.parentNode.insertBefore(t,n),setTimeout(y,2e3,k+1))}})(0))
}}(window,document,""script"",""subiz"",""{_options.AccountId}"");";
        }

        /// <inheritdoc />
        public string GetSetUserAttributesScript(string? name, string? email, Dictionary<string, string>? additionalAttributes = null)
        {
            if (!IsEnabled)
                return string.Empty;

            var attributes = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(name))
                attributes["name"] = EscapeJavaScript(name);

            if (!string.IsNullOrEmpty(email))
                attributes["email"] = EscapeJavaScript(email);

            if (additionalAttributes != null)
            {
                foreach (var attr in additionalAttributes)
                {
                    attributes[attr.Key] = EscapeJavaScript(attr.Value);
                }
            }

            if (attributes.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append("subiz('setUserAttributes', {");

            var first = true;
            foreach (var attr in attributes)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append($"\"{attr.Key}\": \"{attr.Value}\"");
                first = false;
            }

            sb.Append("});");

            return sb.ToString();
        }

        /// <summary>
        /// Escapes a string for safe use in JavaScript
        /// </summary>
        private static string EscapeJavaScript(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
