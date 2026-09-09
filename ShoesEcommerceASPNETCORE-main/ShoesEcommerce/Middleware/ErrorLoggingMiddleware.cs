using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace ShoesEcommerce.Middleware
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorLoggingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            try
            {
                // Log incoming requests for specific endpoints or all requests in development
                if (ShouldLogRequest(context))
                {
                    await LogIncomingRequest(context);
                }

                await _next(context);

                // Log response details for monitored endpoints
                if (ShouldLogResponse(context))
                {
                    LogResponse(context);
                }

                // Handle specific HTTP error status codes
                if (context.Response.StatusCode >= 400)
                {
                    await HandleHttpError(context);
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
                // Re-throw to let ASP.NET Core handle it properly
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private bool ShouldLogRequest(HttpContext context)
        {
            // Log requests for critical endpoints
            var criticalPaths = new[]
            {
                "/Account/Register",
                "/Account/Login",
                "/Checkout",
                "/Payment",
                "/Cart/AddToCart",
                "/Order"
            };

            // Suppress logging for browser probes and JS error logging endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/.well-known/") ||
                path.Contains("/favicon.ico") ||
                path.Contains("/robots.txt") ||
                path.EndsWith(".map") ||
                path.Contains("/error/logjavascripterror"))
            {
                return false;
            }

            return _env.IsDevelopment() ||
                   criticalPaths.Any(p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ShouldLogResponse(HttpContext context)
        {
            // Skip logging for Chrome DevTools, browser probes, and JS error logging endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/.well-known/") ||
                path.Contains("/favicon.ico") ||
                path.Contains("/robots.txt") ||
                path.EndsWith(".map") ||
                path.Contains("/error/logjavascripterror"))
            {
                return false;
            }

            // Log client errors (4xx) and server errors (5xx)
            return context.Response.StatusCode >= 400;
        }

        private async Task LogIncomingRequest(HttpContext context)
        {
            var requestInfo = new
            {
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                QueryString = context.Request.QueryString.Value,
                ContentType = context.Request.ContentType,
                UserAgent = context.Request.Headers.UserAgent.FirstOrDefault(),
                RemoteIP = GetClientIP(context),
                UserId = GetUserId(context),
                SessionId = context.Session?.Id,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Incoming request: {@RequestInfo}", requestInfo);

            // Log form data for POST requests (excluding sensitive fields)
            if (context.Request.Method == "POST" &&
                context.Request.ContentType?.Contains("application/x-www-form-urlencoded") == true &&
                context.Request.ContentLength > 0)
            {
                await LogFormData(context);
            }
        }

        private async Task LogFormData(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    var formData = ParseFormData(body);
                    if (formData.Any())
                    {
                        _logger.LogInformation("Form data: {@FormData}", formData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error logging form data");
            }
        }

        private Dictionary<string, string> ParseFormData(string body)
        {
            var formData = new Dictionary<string, string>();
            var pairs = body.Split('&');

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = Uri.UnescapeDataString(keyValue[0]);
                    var value = Uri.UnescapeDataString(keyValue[1]);

                    // Don't log sensitive fields
                    if (IsSensitiveField(key))
                    {
                        formData[key] = "[REDACTED]";
                    }
                    else
                    {
                        formData[key] = value.Length > 100 ? $"{value.Substring(0, 100)}..." : value;
                    }
                }
            }

            return formData;
        }

        private void LogResponse(HttpContext context)
        {
            var responseInfo = new
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType,
                ContentLength = context.Response.ContentLength,
                Headers = context.Response.Headers
                    .Where(h => !h.Key.StartsWith("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(h => h.Key, h => h.Value.ToString()),
                UserId = GetUserId(context),
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                Timestamp = DateTime.UtcNow
            };

            var logLevel = context.Response.StatusCode >= 500 ? LogLevel.Error :
                          context.Response.StatusCode >= 400 ? LogLevel.Warning :
                          LogLevel.Information;

            _logger.Log(logLevel, "Response: {@ResponseInfo}", responseInfo);
        }

        private async Task HandleHttpError(HttpContext context)
        {
            var errorInfo = new
            {
                StatusCode = context.Response.StatusCode,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.Value,
                UserAgent = context.Request.Headers.UserAgent.FirstOrDefault(),
                RemoteIP = GetClientIP(context),
                UserId = GetUserId(context),
                Referrer = context.Request.Headers.Referer.FirstOrDefault(),
                Timestamp = DateTime.UtcNow
            };

            var statusCode = (HttpStatusCode)context.Response.StatusCode;

            switch (statusCode)
            {
                case HttpStatusCode.NotFound:
                    _logger.LogWarning("404 Not Found: {@ErrorInfo}", errorInfo);
                    break;
                case HttpStatusCode.Unauthorized:
                    _logger.LogWarning("401 Unauthorized access attempt: {@ErrorInfo}", errorInfo);
                    break;
                case HttpStatusCode.Forbidden:
                    _logger.LogWarning("403 Forbidden access attempt: {@ErrorInfo}", errorInfo);
                    break;
                case HttpStatusCode.BadRequest:
                    _logger.LogWarning("400 Bad Request: {@ErrorInfo}", errorInfo);
                    break;
                case HttpStatusCode.InternalServerError:
                    _logger.LogError("500 Internal Server Error: {@ErrorInfo}", errorInfo);
                    break;
                default:
                    if (context.Response.StatusCode >= 500)
                    {
                        _logger.LogError("HTTP {StatusCode} Server Error: {@ErrorInfo}", context.Response.StatusCode, errorInfo);
                    }
                    else if (context.Response.StatusCode >= 400)
                    {
                        _logger.LogWarning("HTTP {StatusCode} Client Error: {@ErrorInfo}", context.Response.StatusCode, errorInfo);
                    }
                    break;
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var errorId = Guid.NewGuid().ToString();

            var errorDetails = new
            {
                ErrorId = errorId,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.Value,
                Headers = context.Request.Headers
                    .Where(h => !IsSensitiveHeader(h.Key))
                    .ToDictionary(h => h.Key, h => h.Value.ToString()),
                UserAgent = context.Request.Headers.UserAgent.FirstOrDefault(),
                RemoteIP = GetClientIP(context),
                UserId = GetUserId(context),
                SessionId = context.Session?.Id,
                ExceptionType = ex.GetType().FullName,
                ExceptionMessage = ex.Message,
                InnerException = ex.InnerException?.Message,
                StackTrace = ex.StackTrace
            };

            // Log different exception types with different severity
            switch (ex)
            {
                case UnauthorizedAccessException:
                    _logger.LogWarning(ex, "Unauthorized access attempt - Error ID: {ErrorId}, Details: {@ErrorDetails}", errorId, errorDetails);
                    break;
                case ArgumentException:
                    _logger.LogWarning(ex, "Invalid argument exception - Error ID: {ErrorId}, Details: {@ErrorDetails}", errorId, errorDetails);
                    break;
                case InvalidOperationException when ex.Message.Contains("database") || ex.Message.Contains("connection"):
                    _logger.LogCritical(ex, "Database connection error - Error ID: {ErrorId}, Details: {@ErrorDetails}", errorId, errorDetails);
                    break;
                case TimeoutException:
                    _logger.LogError(ex, "Timeout error - Error ID: {ErrorId}, Details: {@ErrorDetails}", errorId, errorDetails);
                    break;
                default:
                    _logger.LogError(ex, "Unhandled exception - Error ID: {ErrorId}, Details: {@ErrorDetails}", errorId, errorDetails);
                    break;
            }
        }

        private string GetClientIP(HttpContext context)
        {
            // Check for forwarded IP first (for load balancers, proxies)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIP))
            {
                return realIP;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserId(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                return context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Authenticated";
            }
            return "Anonymous";
        }

        private static bool IsSensitiveField(string fieldName)
        {
            var sensitiveFields = new[]
            {
                "password",
                "confirmpassword",
                "__requestverificationtoken",
                "creditcard",
                "cvv",
                "ssn",
                "socialsecurity"
            };
            return sensitiveFields.Contains(fieldName.ToLowerInvariant());
        }

        private static bool IsSensitiveHeader(string headerName)
        {
            var sensitiveHeaders = new[]
            {
                "authorization",
                "cookie",
                "x-api-key",
                "x-auth-token"
            };
            return sensitiveHeaders.Contains(headerName.ToLowerInvariant());
        }
    }

    // Extension method for easy registration
    public static class ErrorLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorLoggingMiddleware>();
        }
    }
}