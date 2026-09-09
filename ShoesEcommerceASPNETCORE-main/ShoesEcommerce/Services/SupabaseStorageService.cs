using Microsoft.Extensions.Options;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace ShoesEcommerce.Services
{
    /// <summary>
    /// Supabase Storage service using REST API (not S3)
    /// Simple and reliable for files up to 50MB
    /// </summary>
    public class SupabaseStorageService : IStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseStorageOptions _options;
        private readonly ILogger<SupabaseStorageService> _logger;

        public SupabaseStorageService(
            IOptions<SupabaseStorageOptions> options,
            ILogger<SupabaseStorageService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _options = options.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("SupabaseStorage");

            // Log current environment for debugging
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            _logger.LogInformation("🔧 Current Environment: {Environment}", env ?? "Unknown");
            _logger.LogInformation("📦 Supabase Storage Configuration:");
            _logger.LogInformation("   - Project URL: {ProjectUrl}", _options.ProjectUrl);
            _logger.LogInformation("   - Bucket: {BucketName}", _options.BucketName);
            _logger.LogInformation("   - API Key: {KeyPrefix}***", 
                string.IsNullOrEmpty(_options.ServiceRoleKey) ? "NOT SET" : _options.ServiceRoleKey.Substring(0, Math.Min(20, _options.ServiceRoleKey.Length)));

            // Validate configuration
            if (string.IsNullOrEmpty(_options.ProjectUrl) || string.IsNullOrEmpty(_options.ServiceRoleKey))
            {
                _logger.LogWarning("⚠️ Supabase Storage configuration is incomplete. Upload will fail.");
            }
            else
            {
                _logger.LogInformation("✅ Supabase Storage Service initialized successfully");
            }
        }

        public async Task<StorageUploadResult> UploadFileAsync(IFormFile file, string? folder = null, string? customFileName = null)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return StorageUploadResult.FailureResult("File is empty or null");
                }

                if (file.Length > _options.MaxFileSizeBytes)
                {
                    var maxSizeMB = _options.MaxFileSizeBytes / (1024 * 1024);
                    return StorageUploadResult.FailureResult($"File size exceeds maximum allowed size of {maxSizeMB}MB");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_options.AllowedImageExtensions.Contains(extension))
                {
                    return StorageUploadResult.FailureResult($"File type {extension} is not allowed");
                }

                // Generate unique filename - sanitize properly (ASCII only!)
                var fileName = customFileName ?? Path.GetFileNameWithoutExtension(file.FileName);
                fileName = SanitizeToAscii(fileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid():N}{extension}";
                
                // Build the object path
                var sanitizedFolder = string.IsNullOrEmpty(folder) ? "" : SanitizeFolderPath(folder);
                var objectPath = string.IsNullOrEmpty(sanitizedFolder) 
                    ? uniqueFileName 
                    : $"{sanitizedFolder}/{uniqueFileName}";

                _logger.LogInformation("Uploading to Supabase Storage: {Path}", objectPath);

                // Read file into memory
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Upload using Supabase REST API
                var uploadUrl = $"{_options.ProjectUrl}/storage/v1/object/{_options.BucketName}/{objectPath}";

                using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
                request.Headers.Add("x-upsert", "true"); // Overwrite if exists
                
                var content = new ByteArrayContent(fileBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var publicUrl = GetPublicUrl(objectPath);
                    _logger.LogInformation("✅ File uploaded successfully: {Url}", publicUrl);
                    return StorageUploadResult.SuccessResult(publicUrl, objectPath, file.Length, file.ContentType ?? "application/octet-stream");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Upload failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return StorageUploadResult.FailureResult($"Upload failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error uploading file: {FileName}", file?.FileName);
                return StorageUploadResult.FailureResult($"Upload error: {ex.Message}");
            }
        }

        public async Task<StorageUploadResult> UploadFileAsync(Stream stream, string fileName, string contentType, string? folder = null)
        {
            try
            {
                if (stream == null || stream.Length == 0)
                {
                    return StorageUploadResult.FailureResult("Stream is empty or null");
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var sanitizedName = SanitizeToAscii(Path.GetFileNameWithoutExtension(fileName));
                var uniqueFileName = $"{sanitizedName}_{Guid.NewGuid():N}{extension}";
                
                var sanitizedFolder = string.IsNullOrEmpty(folder) ? "" : SanitizeFolderPath(folder);
                var objectPath = string.IsNullOrEmpty(sanitizedFolder) 
                    ? uniqueFileName 
                    : $"{sanitizedFolder}/{uniqueFileName}";

                // Read stream to bytes
                using var memoryStream = new MemoryStream();
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Upload using Supabase REST API
                var uploadUrl = $"{_options.ProjectUrl}/storage/v1/object/{_options.BucketName}/{objectPath}";

                using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
                request.Headers.Add("x-upsert", "true");
                
                var content = new ByteArrayContent(fileBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var publicUrl = GetPublicUrl(objectPath);
                    _logger.LogInformation("✅ File uploaded from stream: {Path}", objectPath);
                    return StorageUploadResult.SuccessResult(publicUrl, objectPath, fileBytes.Length, contentType);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StorageUploadResult.FailureResult($"Upload failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error uploading file from stream: {FileName}", fileName);
                return StorageUploadResult.FailureResult($"Upload error: {ex.Message}");
            }
        }

        public async Task<StorageUploadResult> UploadFileAsync(byte[] data, string fileName, string contentType, string? folder = null)
        {
            using var stream = new MemoryStream(data);
            return await UploadFileAsync(stream, fileName, contentType, folder);
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                var objectPath = ExtractPathFromUrl(fileUrl);
                if (string.IsNullOrEmpty(objectPath))
                {
                    _logger.LogWarning("Could not extract object path from URL: {Url}", fileUrl);
                    return false;
                }

                var deleteUrl = $"{_options.ProjectUrl}/storage/v1/object/{_options.BucketName}/{objectPath}";

                using var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("File deleted: {Path}", objectPath);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to delete file: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Url}", fileUrl);
                return false;
            }
        }

        public async Task<bool> DeleteFilesAsync(IEnumerable<string> fileUrls)
        {
            var results = await Task.WhenAll(fileUrls.Select(DeleteFileAsync));
            return results.All(r => r);
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            try
            {
                var objectPath = ExtractPathFromUrl(fileUrl);
                if (string.IsNullOrEmpty(objectPath))
                {
                    return false;
                }

                // Use HEAD request to check if file exists
                var checkUrl = $"{_options.ProjectUrl}/storage/v1/object/{_options.BucketName}/{objectPath}";

                using var request = new HttpRequestMessage(HttpMethod.Head, checkUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists: {Url}", fileUrl);
                return false;
            }
        }

        public string GetPublicUrl(string objectPath)
        {
            // Supabase public URL format: {ProjectUrl}/storage/v1/object/public/{BucketName}/{Path}
            return $"{_options.ProjectUrl}/storage/v1/object/public/{_options.BucketName}/{objectPath}";
        }

        public Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan expiration)
        {
            // For public buckets, just return the public URL
            // For private buckets, you'd need to implement signed URL generation
            return Task.FromResult(GetPublicUrl(fileKey));
        }

        private string? ExtractPathFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            // If it's already a path (not a URL), return as is
            if (!url.StartsWith("http"))
            {
                return url;
            }

            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                
                // Remove leading /storage/v1/object/public/{bucket}/
                var prefix = $"/storage/v1/object/public/{_options.BucketName}/";
                if (path.StartsWith(prefix))
                {
                    return path.Substring(prefix.Length);
                }

                // Try alternative prefix
                var altPrefix = "/storage/v1/object/public/";
                if (path.StartsWith(altPrefix))
                {
                    var remaining = path.Substring(altPrefix.Length);
                    var slashIndex = remaining.IndexOf('/');
                    if (slashIndex > 0)
                    {
                        return remaining.Substring(slashIndex + 1);
                    }
                }

                return Path.GetFileName(path);
            }
            catch
            {
                return Path.GetFileName(url);
            }
        }

        /// <summary>
        /// Sanitize text to ASCII only - removes all non-ASCII characters including Vietnamese diacritics
        /// </summary>
        private string SanitizeToAscii(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "file";

            // Normalize the string to decompose characters (separate base chars from diacritics)
            var normalized = text.Normalize(NormalizationForm.FormD);
            
            // Build result with only ASCII letters, digits, underscore and hyphen
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                
                // Skip combining marks (diacritics)
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;
                
                // Keep ASCII letters and digits
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                }
                // Replace Vietnamese đ with d
                else if (c == '\u0111' || c == '\u0110')
                {
                    sb.Append('d');
                }
                // Replace spaces and punctuation with underscore
                else if (c == ' ' || c == '-' || c == '_')
                {
                    sb.Append('_');
                }
            }

            var result = sb.ToString().ToLowerInvariant();
            
            // Clean up multiple underscores
            while (result.Contains("__"))
            {
                result = result.Replace("__", "_");
            }
            
            result = result.Trim('_');

            // Limit length
            if (result.Length > 50)
            {
                result = result.Substring(0, 50);
            }

            return string.IsNullOrEmpty(result) ? "file" : result;
        }

        /// <summary>
        /// Sanitize folder path - each segment must be ASCII only
        /// </summary>
        private string SanitizeFolderPath(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return "";

            var parts = folder.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var sanitizedParts = parts.Select(p => SanitizeToAscii(p)).Where(p => !string.IsNullOrEmpty(p));
            return string.Join("/", sanitizedParts);
        }
    }
}
