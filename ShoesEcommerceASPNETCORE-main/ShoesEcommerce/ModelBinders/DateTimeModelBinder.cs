using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace ShoesEcommerce.ModelBinders
{
    /// <summary>
    /// Custom model binder for DateTime to handle HTML5 date inputs properly
    /// Ensures yyyy-MM-dd format from HTML5 date inputs is correctly parsed
    /// </summary>
    public class DateTimeModelBinder : IModelBinder
    {
        private readonly ILogger<DateTimeModelBinder> _logger;

        public DateTimeModelBinder(ILogger<DateTimeModelBinder> logger)
        {
            _logger = logger;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (value == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);

            var stringValue = value.FirstValue;
            if (string.IsNullOrEmpty(stringValue))
            {
                return Task.CompletedTask;
            }

            _logger.LogDebug("?? DateTimeModelBinder: Binding '{ModelName}' with value '{Value}'", 
                bindingContext.ModelName, stringValue);

            DateTime result;

            // Try to parse HTML5 date format first (yyyy-MM-dd)
            if (DateTime.TryParseExact(stringValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                _logger.LogDebug("? Successfully parsed HTML5 date format: {Date}", result);
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }

            // Try to parse HTML5 datetime-local format (yyyy-MM-ddTHH:mm)
            if (DateTime.TryParseExact(stringValue, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                _logger.LogDebug("? Successfully parsed HTML5 datetime-local format: {DateTime}", result);
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }

            // Try other common formats
            string[] formats = {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd",
                "dd/MM/yyyy",
                "MM/dd/yyyy",
                "dd-MM-yyyy",
                "MM-dd-yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    _logger.LogDebug("? Successfully parsed with format '{Format}': {Date}", format, result);
                    bindingContext.Result = ModelBindingResult.Success(result);
                    return Task.CompletedTask;
                }
            }

            // Fallback to default parsing
            if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                _logger.LogDebug("? Successfully parsed with default parsing: {Date}", result);
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }

            // If all parsing attempts fail
            _logger.LogWarning("? Failed to parse date: '{Value}' for model '{ModelName}'", stringValue, bindingContext.ModelName);
            bindingContext.Result = ModelBindingResult.Failed();
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "??nh d?ng ngày không h?p l?");

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Model binder provider for the DateTime model binder
    /// </summary>
    public class DateTimeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(DateTime) || 
                context.Metadata.ModelType == typeof(DateTime?))
            {
                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<DateTimeModelBinder>();
                return new DateTimeModelBinder(logger);
            }

            return null;
        }
    }
}