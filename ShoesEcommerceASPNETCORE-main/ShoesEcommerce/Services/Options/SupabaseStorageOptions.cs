namespace ShoesEcommerce.Services.Options
{
    /// <summary>
    /// Configuration options for Supabase Storage using REST API
    /// </summary>
    public class SupabaseStorageOptions
    {
        public const string SectionName = "SupabaseStorage";

        /// <summary>
        /// Supabase project URL (e.g., https://xxxxx.supabase.co)
        /// </summary>
        public string ProjectUrl { get; set; } = string.Empty;

        /// <summary>
        /// Supabase service_role key for server-side uploads
        /// Get this from Supabase Dashboard -> Settings -> API -> service_role key
        /// </summary>
        public string ServiceRoleKey { get; set; } = string.Empty;

        /// <summary>
        /// Default bucket name for storing files
        /// </summary>
        public string BucketName { get; set; } = "images";

        /// <summary>
        /// Base URL for accessing public files
        /// </summary>
        public string PublicUrl => $"{ProjectUrl}/storage/v1/object/public/{BucketName}";

        /// <summary>
        /// Maximum file size in bytes (default: 50MB for Supabase free tier)
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

        /// <summary>
        /// Allowed file extensions for images
        /// </summary>
        public string[] AllowedImageExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    }
}
