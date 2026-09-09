using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Models;
using System.Diagnostics;

namespace ShoesEcommerce.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("Error")]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            if (exception != null)
            {
                _logger.LogError(exception, "Unhandled exception occurred. Request ID: {RequestId}, Path: {Path}", 
                    requestId, exceptionHandlerPathFeature?.Path);

                // Log additional context information
                var requestDetails = new
                {
                    RequestId = requestId,
                    Path = Request.Path,
                    Method = Request.Method,
                    QueryString = Request.QueryString.Value,
                    UserAgent = Request.Headers.UserAgent.FirstOrDefault(),
                    Referrer = Request.Headers.Referer.FirstOrDefault(),
                    UserId = User?.Identity?.IsAuthenticated == true ? User.FindFirst("sub")?.Value : "Anonymous",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ExceptionType = exception.GetType().Name,
                    ExceptionMessage = exception.Message,
                    StackTrace = exception.StackTrace
                };

                _logger.LogError("Error context details: {@RequestDetails}", requestDetails);
            }

            var model = new ErrorViewModel 
            { 
                RequestId = requestId
            };

            Response.StatusCode = 500;
            return View(model);
        }

        [Route("Error/404")]
        public IActionResult NotFound()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            _logger.LogWarning("404 Not Found - Request ID: {RequestId}, Path: {Path}, Method: {Method}, UserAgent: {UserAgent}", 
                requestId, Request.Path, Request.Method, Request.Headers.UserAgent.FirstOrDefault());

            Response.StatusCode = 404;
            ViewData["RequestId"] = requestId;
            
            return View("NotFound");
        }

        [Route("Error/403")]
        public IActionResult Forbidden()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            _logger.LogWarning("403 Forbidden - Request ID: {RequestId}, Path: {Path}, Method: {Method}, User: {UserId}", 
                requestId, Request.Path, Request.Method, User?.Identity?.Name ?? "Anonymous");

            Response.StatusCode = 403;
            ViewData["RequestId"] = requestId;
            
            return View("Forbidden");
        }

        [Route("Error/{statusCode}")]
        public IActionResult HandleErrorCode(int statusCode)
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            _logger.LogWarning("HTTP {StatusCode} error - Request ID: {RequestId}, Path: {Path}, Method: {Method}", 
                statusCode, requestId, Request.Path, Request.Method);

            Response.StatusCode = statusCode;
            ViewData["RequestId"] = requestId;
            ViewData["StatusCode"] = statusCode;

            return statusCode switch
            {
                404 => View("NotFound"),
                403 => View("Forbidden"),
                401 => View("Unauthorized"),
                400 => View("BadRequest"),
                _ => View("Error", new ErrorViewModel { RequestId = requestId })
            };
        }

        // API endpoint for JavaScript error reporting
        [HttpPost]
        [Route("Error/LogJavaScriptError")]
        public IActionResult LogJavaScriptError([FromBody] JavaScriptErrorModel model)
        {
            try
            {
                if (model != null)
                {
                    _logger.LogError("JavaScript Error - Message: {Message}, Source: {Source}, Line: {Line}, Column: {Column}, Stack: {Stack}, URL: {Url}, UserAgent: {UserAgent}", 
                        model.Message, 
                        model.Source, 
                        model.Line, 
                        model.Column, 
                        model.Stack, 
                        model.Url, 
                        Request.Headers.UserAgent.FirstOrDefault());
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging JavaScript error");
                return StatusCode(500);
            }
        }
    }

    public class JavaScriptErrorModel
    {
        public string? Message { get; set; }
        public string? Source { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string? Stack { get; set; }
        public string? Url { get; set; }
    }
}