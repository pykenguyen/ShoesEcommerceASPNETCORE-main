namespace ShoesEcommerce.Services.Interfaces
{
    public interface IFileUploadService
    {
        /// <summary>
        /// Upload an image file to the specified subfolder
        /// </summary>
        /// <param name="file">The image file to upload</param>
        /// <param name="subFolder">Optional subfolder path</param>
        /// <returns>Relative URL of the uploaded image</returns>
        Task<string> UploadImageAsync(IFormFile file, string subFolder = "");

        /// <summary>
        /// Upload a product variant image with organized naming
        /// </summary>
        /// <param name="file">The image file to upload</param>
        /// <param name="productId">Product ID for organization</param>
        /// <param name="color">Variant color for naming</param>
        /// <param name="size">Variant size for naming</param>
        /// <returns>Relative URL of the uploaded image</returns>
        Task<string> UploadProductVariantImageAsync(IFormFile file, int productId, string color, string size);

        /// <summary>
        /// Delete an image file by its URL
        /// </summary>
        /// <param name="imageUrl">Relative URL of the image to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteImageAsync(string imageUrl);

        /// <summary>
        /// Validate image file format, size, and type
        /// </summary>
        /// <param name="file">The file to validate</param>
        /// <returns>Validation result with error details if invalid</returns>
        ValidationResult ValidateImageFile(IFormFile file);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}