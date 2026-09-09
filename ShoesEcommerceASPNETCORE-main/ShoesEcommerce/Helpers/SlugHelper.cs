using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ShoesEcommerce.Helpers
{
    /// <summary>
    /// Helper class for generating SEO-friendly URL slugs
    /// </summary>
    public static class SlugHelper
    {
        /// <summary>
        /// Converts a string to a URL-friendly slug
        /// </summary>
        public static string ToSlug(this string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            // Convert to lowercase first
            title = title.ToLowerInvariant();

            // Replace Vietnamese characters
            title = ReplaceVietnameseChars(title);

            // Remove diacritical marks
            title = title.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in title)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            title = sb.ToString().Normalize(NormalizationForm.FormC);

            // Replace special characters
            title = title.Replace("&", "and")
                        .Replace("@", "at")
                        .Replace("#", "sharp")
                        .Replace("+", "plus");

            // Remove any characters that are not alphanumeric, spaces, or hyphens
            title = Regex.Replace(title, @"[^a-z0-9\s-]", "");

            // Replace multiple spaces with single space
            title = Regex.Replace(title, @"\s+", " ");

            // Trim and replace spaces with hyphens
            title = title.Trim().Replace(' ', '-');

            // Remove duplicate hyphens
            title = Regex.Replace(title, @"-+", "-");

            // Trim leading/trailing hyphens
            return title.Trim('-');
        }

        /// <summary>
        /// Replace Vietnamese characters with ASCII equivalents
        /// </summary>
        private static string ReplaceVietnameseChars(string str)
        {
            str = Regex.Replace(str, "[àá??ãâ???????????]", "a");
            str = Regex.Replace(str, "[èé???ê?????]", "e");
            str = Regex.Replace(str, "[ìí???]", "i");
            str = Regex.Replace(str, "[òó??õô???????????]", "o");
            str = Regex.Replace(str, "[ùú?????????]", "u");
            str = Regex.Replace(str, "[?ý???]", "y");
            str = Regex.Replace(str, "[?]", "d");
            str = Regex.Replace(str, "[ÀÁ??ÃÂ???????????]", "a");
            str = Regex.Replace(str, "[ÈÉ???Ê?????]", "e");
            str = Regex.Replace(str, "[ÌÍ???]", "i");
            str = Regex.Replace(str, "[ÒÓ??ÕÔ???????????]", "o");
            str = Regex.Replace(str, "[ÙÚ?????????]", "u");
            str = Regex.Replace(str, "[?Ý???]", "y");
            str = Regex.Replace(str, "[?]", "d");
            return str;
        }

        /// <summary>
        /// Generates a unique slug by appending an ID
        /// </summary>
        public static string ToSlugWithId(this string title, int id)
        {
            var slug = title.ToSlug();
            return string.IsNullOrEmpty(slug) ? id.ToString() : $"{slug}-{id}";
        }

        /// <summary>
        /// Generates full SEO-friendly URL path with category
        /// Example: /giay-da-bong/nike-mercurial-vapor-15-28
        /// </summary>
        public static string ToFullProductUrl(string productName, string? categoryName, string? color, int productId)
        {
            var categorySlug = !string.IsNullOrEmpty(categoryName) ? categoryName.ToSlug() : "san-pham";
            var productSlug = productName.ToSlug();
            
            // Optionally include color in slug
            var slug = !string.IsNullOrEmpty(color) 
                ? $"{productSlug}-{color.ToSlug()}-{productId}"
                : $"{productSlug}-{productId}";
            
            return $"/{categorySlug}/{slug}";
        }

        /// <summary>
        /// Extracts the ID from a slug that ends with -id format
        /// </summary>
        public static int ExtractIdFromSlug(string slugWithId)
        {
            if (string.IsNullOrWhiteSpace(slugWithId))
                return 0;

            // Try to extract ID from the end of the slug
            var lastDashIndex = slugWithId.LastIndexOf('-');
            if (lastDashIndex > 0 && lastDashIndex < slugWithId.Length - 1)
            {
                var idPart = slugWithId.Substring(lastDashIndex + 1);
                if (int.TryParse(idPart, out int id))
                {
                    return id;
                }
            }

            // If the slug is just a number
            if (int.TryParse(slugWithId, out int directId))
            {
                return directId;
            }

            return 0;
        }

        /// <summary>
        /// Validates if a slug matches the expected format
        /// </summary>
        public static bool ValidateSlug(string slug, string expectedTitle, int expectedId)
        {
            var expectedSlug = expectedTitle.ToSlugWithId(expectedId);
            return string.Equals(slug, expectedSlug, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Generates a product URL with optional variant parameters for social sharing
        /// </summary>
        public static string ToProductUrl(string baseUrl, string productName, int productId, string? color = null, string? size = null)
        {
            var slug = productName.ToSlugWithId(productId);
            var url = $"{baseUrl.TrimEnd('/')}/san-pham/{slug}";
            
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(color))
            {
                queryParams.Add($"color={Uri.EscapeDataString(color)}");
            }
            
            if (!string.IsNullOrEmpty(size))
            {
                queryParams.Add($"size={Uri.EscapeDataString(size)}");
            }
            
            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }
            
            return url;
        }

        /// <summary>
        /// Generates a share-friendly title including variant info
        /// </summary>
        public static string ToShareTitle(string productName, string? brandName = null, string? color = null, string? size = null)
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(brandName))
            {
                parts.Add(brandName);
            }
            
            parts.Add(productName);
            
            var variantParts = new List<string>();
            if (!string.IsNullOrEmpty(color))
            {
                variantParts.Add(color);
            }
            if (!string.IsNullOrEmpty(size))
            {
                variantParts.Add($"Size {size}");
            }
            
            if (variantParts.Count > 0)
            {
                parts.Add($"({string.Join(" - ", variantParts)})");
            }
            
            return string.Join(" ", parts);
        }
    }
}
