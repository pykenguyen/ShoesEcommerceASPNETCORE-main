using System.Text.RegularExpressions;

namespace ShoesEcommerce.Middleware
{
    /// <summary>
    /// Middleware to allow social media crawlers (Facebook, Twitter, etc.) 
    /// to access pages for Open Graph tag scraping without authentication or redirects
    /// </summary>
    public class SocialCrawlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SocialCrawlerMiddleware> _logger;

        // Known social media crawler user agents
        private static readonly string[] SocialCrawlerPatterns = new[]
        {
            "facebookexternalhit",
            "Facebot",
            "Twitterbot",
            "LinkedInBot",
            "Pinterest",
            "Slackbot",
            "TelegramBot",
            "WhatsApp",
            "Discordbot",
            "Googlebot",
            "bingbot"
        };

        public SocialCrawlerMiddleware(RequestDelegate next, ILogger<SocialCrawlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "";

            // Check if this is a social media crawler
            var isSocialCrawler = SocialCrawlerPatterns.Any(pattern => 
                userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            if (isSocialCrawler)
            {
                var path = context.Request.Path.Value ?? "";
                var queryString = context.Request.QueryString.Value ?? "";
                
                _logger.LogInformation(
                    "?? Social crawler detected: {UserAgent}, Path: {Path}{Query}",
                    userAgent.Length > 50 ? userAgent.Substring(0, 50) + "..." : userAgent, 
                    path,
                    queryString);

                // Set flags to indicate this is a social crawler request
                context.Items["IsSocialCrawler"] = true;
                context.Items["AllowAnonymous"] = true;
                
                // IMPORTANT: Skip redirect for social crawlers on product pages
                // This prevents redirect loops that cause 403 errors
                if (path.StartsWith("/san-pham", StringComparison.OrdinalIgnoreCase) || 
                    path.StartsWith("/product", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/khuyen-mai", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/giay-", StringComparison.OrdinalIgnoreCase) || // Category-based URLs
                    path == "/" ||
                    path.StartsWith("/home", StringComparison.OrdinalIgnoreCase))
                {
                    context.Items["SkipCanonicalRedirect"] = true;
                }
            }

            await _next(context);

            // Log if we returned an error to a social crawler
            if (isSocialCrawler)
            {
                var statusCode = context.Response.StatusCode;
                if (statusCode >= 400)
                {
                    _logger.LogWarning(
                        "?? Social crawler received error: {StatusCode}, UserAgent: {UserAgent}, Path: {Path}",
                        statusCode, 
                        userAgent.Length > 30 ? userAgent.Substring(0, 30) + "..." : userAgent, 
                        context.Request.Path);
                }
                else if (statusCode >= 300 && statusCode < 400)
                {
                    var location = context.Response.Headers.Location.FirstOrDefault();
                    _logger.LogInformation(
                        "?? Social crawler redirected: {StatusCode} ? {Location}",
                        statusCode, location);
                }
                else
                {
                    _logger.LogInformation(
                        "? Social crawler served successfully: {StatusCode}, Path: {Path}",
                        statusCode, context.Request.Path);
                }
            }
        }
    }

    /// <summary>
    /// Extension method for registering the middleware
    /// </summary>
    public static class SocialCrawlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseSocialCrawlerSupport(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SocialCrawlerMiddleware>();
        }
    }
}
