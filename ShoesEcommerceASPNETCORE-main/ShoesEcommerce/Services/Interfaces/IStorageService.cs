namespace ShoesEcommerce.Services.Interfaces
{
    /// <summary>
    /// Interface for cloud storage operations (Supabase S3, AWS S3, etc.)
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Upload a file to cloud storage
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <param name="folder">Optional folder path within the bucket</param>
        /// <param name="customFileName">Optional custom filename (without extension)</param>
        /// <returns>The public URL of the uploaded file</returns>
        Task<StorageUploadResult> UploadFileAsync(IFormFile file, string? folder = null, string? customFileName = null);

        /// <summary>
        /// Upload a file from a stream
        /// </summary>
        Task<StorageUploadResult> UploadFileAsync(Stream stream, string fileName, string contentType, string? folder = null);

        /// <summary>
        /// Upload a file from byte array
        /// </summary>
        Task<StorageUploadResult> UploadFileAsync(byte[] data, string fileName, string contentType, string? folder = null);

        /// <summary>
        /// Delete a file from cloud storage
        /// </summary>
        /// <param name="fileUrl">The URL or key of the file to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteFileAsync(string fileUrl);

        /// <summary>
        /// Delete multiple files from cloud storage
        /// </summary>
        Task<bool> DeleteFilesAsync(IEnumerable<string> fileUrls);

        /// <summary>
        /// Check if a file exists in cloud storage
        /// </summary>
        Task<bool> FileExistsAsync(string fileUrl);

        /// <summary>
        /// Get the public URL for a file
        /// </summary>
        string GetPublicUrl(string fileKey);

        /// <summary>
        /// Generate a pre-signed URL for temporary access
        /// </summary>
        Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan expiration);
    }

    /// <summary>
    /// Result of a storage upload operation
    /// </summary>
    public class StorageUploadResult
    {
        public bool Success { get; set; }
        public string? Url { get; set; }
        public string? Key { get; set; }
        public string? ErrorMessage { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }

        public static StorageUploadResult SuccessResult(string url, string key, long fileSize, string contentType)
        {
            return new StorageUploadResult
            {
                Success = true,
                Url = url,
                Key = key,
                FileSize = fileSize,
                ContentType = contentType
            };
        }

        public static StorageUploadResult FailureResult(string errorMessage)
        {
            return new StorageUploadResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
