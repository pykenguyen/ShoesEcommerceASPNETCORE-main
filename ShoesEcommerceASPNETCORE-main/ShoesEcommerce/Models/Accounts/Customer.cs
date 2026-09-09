using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Carts;

namespace ShoesEcommerce.Models.Accounts
{
    public class Customer
    {
        
        public int Id { get; set; }


        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Vietnamese phone number must be 10 digits starting with 03, 05, 07, 08, or 09")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Url]
        public string? ImageUrl { get; set; }

        [MaxLength(100)]    
        public string? Address { get; set; }
        
        [MaxLength(50)]
        public string? City { get; set; }
        
        [MaxLength(50)]
        public string? State { get; set; }

        // OAuth Provider Information
        [MaxLength(100)]
        public string? GoogleId { get; set; }

        [MaxLength(50)]
        public string? AuthProvider { get; set; } // "Local", "Google", "Facebook", etc.

        public bool IsExternalLogin => !string.IsNullOrEmpty(AuthProvider) && AuthProvider != "Local";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public int? CartId { get; set; }
        public Cart? Cart { get; set; }
        public ICollection<Order>? Orders { get; set; }
        public ICollection<ShippingAddress>? ShippingAddresses { get; set; }
        public ICollection<Comment>? Comments { get; set; }
        public ICollection<QA>? QAs { get; set; }
        public ICollection<Favorite>? Favorites { get; set; }
        public ICollection<UserRole>? Roles { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
