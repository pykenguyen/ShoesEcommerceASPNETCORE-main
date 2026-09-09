using Microsoft.Extensions.Options;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services.Options;

namespace ShoesEcommerce.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly IStorageService _storageService;
        private readonly SupabaseStorageOptions _storageOptions;
        private readonly bool _useCloudStorage;
        
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private const string ImageFolder = "images";
        private const string ProductVariantFolder = "product-variants";

        public FileUploadService(
            IWebHostEnvironment environment, 
            ILogger<FileUploadService> logger,
            IStorageService storageService,
            IOptions<SupabaseStorageOptions> storageOptions)
        {
            _environment = environment;
            _logger = logger;
            _storageService = storageService;
            _storageOptions = storageOptions.Value;
            
            // Use cloud storage if configured with ServiceRoleKey
            _useCloudStorage = !string.IsNullOrEmpty(_storageOptions.ServiceRoleKey) 
                && !string.IsNullOrEmpty(_storageOptions.ProjectUrl);

            if (_useCloudStorage)
            {
                _logger.LogInformation("☁️ FileUploadService using Supabase cloud storage");
                _logger.LogInformation("   - Project URL: {ProjectUrl}", _storageOptions.ProjectUrl);
                _logger.LogInformation("   - Bucket: {BucketName}", _storageOptions.BucketName);
                _logger.LogInformation("   - Service Role Key: {KeyPrefix}...", 
                    _storageOptions.ServiceRoleKey?.Substring(0, Math.Min(20, _storageOptions.ServiceRoleKey?.Length ?? 0)));
            }
            else
            {
                _logger.LogWarning("📁 FileUploadService using LOCAL file storage - Supabase credentials not configured!");
                _logger.LogWarning("   - ServiceRoleKey configured: {HasKey}", !string.IsNullOrEmpty(_storageOptions.ServiceRoleKey));
                _logger.LogWarning("   - ProjectUrl configured: {HasProjectUrl}", !string.IsNullOrEmpty(_storageOptions.ProjectUrl));
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string subFolder = "")
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is null or empty");

                // Validate file
                var validationResult = ValidateImageFile(file);
                if (!validationResult.IsValid)
                    throw new ArgumentException(validationResult.ErrorMessage);

                // Use cloud storage if available
                if (_useCloudStorage)
                {
                    _logger.LogInformation("?? Uploading to Supabase cloud storage...");
                    return await UploadToCloudAsync(file, subFolder);
                }

                // Fallback to local storage
                _logger.LogInformation("?? Uploading to local storage (cloud not configured)...");
                return await UploadToLocalAsync(file, subFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image file");
                throw;
            }
        }

        public async Task<string> UploadProductVariantImageAsync(IFormFile file, int productId, string color, string size)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file provided for product variant image upload");
                    return string.Empty;
                }

                // Validate file first
                var validationResult = ValidateImageFile(file);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("File validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                    throw new ArgumentException(validationResult.ErrorMessage);
                }

                var subFolder = $"{ProductVariantFolder}/product-{productId}";

                // Use cloud storage if available
                if (_useCloudStorage)
                {
                    _logger.LogInformation("?? Uploading product variant image to Supabase: Product={ProductId}, Color={Color}, Size={Size}", 
                        productId, color, size);
                    
                    var customFileName = GenerateProductVariantFileName(file.FileName, productId, color, size);
                    customFileName = Path.GetFileNameWithoutExtension(customFileName);
                    
                    var result = await _storageService.UploadFileAsync(file, subFolder, customFileName);
                    
                    if (result.Success && !string.IsNullOrEmpty(result.Url))
                    {
                        _logger.LogInformation("? Product variant image uploaded to cloud: {Url}", result.Url);
                        return result.Url;
                    }
                    else
                    {
                        _logger.LogError("? Cloud upload failed: {Error}", result.ErrorMessage);
                        // Don't fallback to local - throw error instead to make issue visible
                        throw new InvalidOperationException($"Failed to upload to Supabase: {result.ErrorMessage}");
                    }
                }

                // Fallback to local storage only if cloud is not configured
                _logger.LogWarning("?? Using local storage fallback - Supabase not configured");
                return await UploadProductVariantToLocalAsync(file, productId, color, size);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product variant image for Product {ProductId}, Color: {Color}, Size: {Size}", 
                    productId, color, size);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return false;

                // Check if it's a cloud URL
                if (imageUrl.StartsWith("http"))
                {
                    _logger.LogInformation("??? Deleting cloud image: {Url}", imageUrl);
                    return await _storageService.DeleteFileAsync(imageUrl);
                }

                // Local file deletion
                if (!imageUrl.StartsWith("/"))
                    return false;

                var physicalPath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
                
                if (File.Exists(physicalPath))
                {
                    await Task.Run(() => File.Delete(physicalPath));
                    _logger.LogInformation("??? Deleted local file: {Path}", physicalPath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {ImageUrl}", imageUrl);
                return false;
            }
        }

        public ValidationResult ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new ValidationResult { IsValid = false, ErrorMessage = "File is required" };

            // Check file size
            if (file.Length > MaxFileSize)
                return new ValidationResult { IsValid = false, ErrorMessage = $"File size must be less than {MaxFileSize / (1024 * 1024)}MB" };

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return new ValidationResult { IsValid = false, ErrorMessage = $"Only {string.Join(", ", _allowedExtensions)} files are allowed" };

            // Check MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return new ValidationResult { IsValid = false, ErrorMessage = "Invalid file type" };

            return new ValidationResult { IsValid = true };
        }

        #region Cloud Storage Methods

        private async Task<string> UploadToCloudAsync(IFormFile file, string subFolder)
        {
            _logger.LogInformation("?? UploadToCloudAsync: folder={SubFolder}, file={FileName}, size={Size}", 
                subFolder, file.FileName, file.Length);
            
            var result = await _storageService.UploadFileAsync(file, subFolder);
            
            if (result.Success && !string.IsNullOrEmpty(result.Url))
            {
                _logger.LogInformation("? Image uploaded to cloud: {Url}", result.Url);
                return result.Url;
            }
            
            _logger.LogError("? Cloud upload failed: {Error}", result.ErrorMessage);
            throw new InvalidOperationException($"Failed to upload to Supabase: {result.ErrorMessage}");
        }

        #endregion

        #region Local Storage Methods

        private async Task<string> UploadToLocalAsync(IFormFile file, string subFolder)
        {
            var uploadPath = CreateUploadDirectory(subFolder);
            var fileName = GenerateUniqueFileName(file.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine(ImageFolder, subFolder, fileName).Replace('\\', '/');
            var imageUrl = $"/{relativePath}";
            
            _logger.LogInformation("?? Image uploaded to local: {Url}", imageUrl);
            return imageUrl;
        }

        private async Task<string> UploadProductVariantToLocalAsync(IFormFile file, int productId, string color, string size)
        {
            var subFolder = Path.Combine(ProductVariantFolder, $"product-{productId}");
            var uploadPath = CreateUploadDirectory(subFolder);

            var fileName = GenerateProductVariantFileName(file.FileName, productId, color, size);
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("File was not saved successfully");
            }

            var relativePath = Path.Combine(ImageFolder, subFolder, fileName).Replace('\\', '/');
            var imageUrl = $"/{relativePath}";
            
            _logger.LogInformation("?? Product variant image uploaded to local: {Url}", imageUrl);
            return imageUrl;
        }

        private string CreateUploadDirectory(string subFolder)
        {
            var uploadPath = Path.Combine(_environment.WebRootPath, ImageFolder);
            
            if (!string.IsNullOrEmpty(subFolder))
            {
                uploadPath = Path.Combine(uploadPath, subFolder);
            }

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
                _logger.LogInformation("?? Directory created: {Path}", uploadPath);
            }

            return uploadPath;
        }

        #endregion

        #region Filename Generation

        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileName = Path.GetFileNameWithoutExtension(originalFileName);
            fileName = SanitizeFileName(fileName);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            
            return $"{fileName}_{timestamp}_{uniqueId}{extension}";
        }

        private string GenerateProductVariantFileName(string originalFileName, int productId, string color, string size)
        {
            var extension = Path.GetExtension(originalFileName);
            var sanitizedColor = SanitizeFileName(color);
            var sanitizedSize = SanitizeFileName(size);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N");
            
            return $"product_{productId}_{sanitizedColor}_{sanitizedSize}_{timestamp}_{uniqueId}{extension}";
        }

        /// <summary>
        /// Sanitize filename - remove Vietnamese diacritics and special characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "file";

            // Remove Vietnamese diacritics
            var result = RemoveVietnameseDiacritics(fileName);
            
            // Remove invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            result = string.Join("", result.Where(c => !invalidChars.Contains(c)));
            
            // Keep only alphanumeric, underscore, and hyphen
            result = new string(result
                .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
                .ToArray());
            
            // Replace multiple underscores
            while (result.Contains("__"))
                result = result.Replace("__", "_");
            
            result = result.Trim('_', '-').ToLowerInvariant();
            
            // Limit length
            if (result.Length > 50)
                result = result.Substring(0, 50);

            return string.IsNullOrEmpty(result) ? "file" : result;
        }

        /// <summary>
        /// Remove Vietnamese diacritics using simple character replacement
        /// </summary>
        private string RemoveVietnameseDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var result = text;
            
            // Lowercase Vietnamese
            result = result.Replace("à", "a").Replace("á", "a").Replace("ạ", "a").Replace("ả", "a").Replace("ã", "a");
            result = result.Replace("â", "a").Replace("ầ", "a").Replace("ấ", "a").Replace("ậ", "a").Replace("ẩ", "a").Replace("ẫ", "a");
            result = result.Replace("ă", "a").Replace("ằ", "a").Replace("ắ", "a").Replace("ặ", "a").Replace("ẳ", "a").Replace("ẵ", "a");
            result = result.Replace("è", "e").Replace("é", "e").Replace("ẹ", "e").Replace("ẻ", "e").Replace("ẽ", "e");
            result = result.Replace("ê", "e").Replace("ề", "e").Replace("ế", "e").Replace("ệ", "e").Replace("ể", "e").Replace("ễ", "e");
            result = result.Replace("ì", "i").Replace("í", "i").Replace("ị", "i").Replace("ỉ", "i").Replace("ĩ", "i");
            result = result.Replace("ò", "o").Replace("ó", "o").Replace("ọ", "o").Replace("ỏ", "o").Replace("õ", "o");
            result = result.Replace("ô", "o").Replace("ồ", "o").Replace("ố", "o").Replace("ộ", "o").Replace("ổ", "o").Replace("ỗ", "o");
            result = result.Replace("ơ", "o").Replace("ờ", "o").Replace("ớ", "o").Replace("ợ", "o").Replace("ở", "o").Replace("ỡ", "o");
            result = result.Replace("ù", "u").Replace("ú", "u").Replace("ụ", "u").Replace("ủ", "u").Replace("ũ", "u");
            result = result.Replace("ư", "u").Replace("ừ", "u").Replace("ứ", "u").Replace("ự", "u").Replace("ử", "u").Replace("ữ", "u");
            result = result.Replace("ỳ", "y").Replace("ý", "y").Replace("ỵ", "y").Replace("ỷ", "y").Replace("ỹ", "y");
            result = result.Replace("đ", "d");
            
            // Uppercase Vietnamese
            result = result.Replace("À", "A").Replace("Á", "A").Replace("Ạ", "A").Replace("Ả", "A").Replace("Ã", "A");
            result = result.Replace("Â", "A").Replace("Ầ", "A").Replace("Ấ", "A").Replace("Ậ", "A").Replace("Ẩ", "A").Replace("Ẫ", "A");
            result = result.Replace("Ă", "A").Replace("Ằ", "A").Replace("Ắ", "A").Replace("Ặ", "A").Replace("Ẳ", "A").Replace("Ẵ", "A");
            result = result.Replace("È", "E").Replace("É", "E").Replace("Ẹ", "E").Replace("Ẻ", "E").Replace("Ẽ", "E");
            result = result.Replace("Ê", "E").Replace("Ề", "E").Replace("Ế", "E").Replace("Ệ", "E").Replace("Ể", "E").Replace("Ễ", "E");
            result = result.Replace("Ì", "I").Replace("Í", "I").Replace("Ị", "I").Replace("Ỉ", "I").Replace("Ĩ", "I");
            result = result.Replace("Ò", "O").Replace("Ó", "O").Replace("Ọ", "O").Replace("Ỏ", "O").Replace("Õ", "O");
            result = result.Replace("Ô", "O").Replace("Ồ", "O").Replace("Ố", "O").Replace("Ộ", "O").Replace("Ổ", "O").Replace("Ỗ", "O");
            result = result.Replace("Ơ", "O").Replace("Ờ", "O").Replace("Ớ", "O").Replace("Ợ", "O").Replace("Ở", "O").Replace("Ỡ", "O");
            result = result.Replace("Ù", "U").Replace("Ú", "U").Replace("Ụ", "U").Replace("Ủ", "U").Replace("Ũ", "U");
            result = result.Replace("Ư", "U").Replace("Ừ", "U").Replace("Ứ", "U").Replace("Ự", "U").Replace("Ử", "U").Replace("Ữ", "U");
            result = result.Replace("Ỳ", "Y").Replace("Ý", "Y").Replace("Ỵ", "Y").Replace("Ỷ", "Y").Replace("Ỹ", "Y");
            result = result.Replace("Đ", "D");
            
            // Replace spaces
            result = result.Replace(" ", "_");

            return result;
        }

        #endregion
    }
}