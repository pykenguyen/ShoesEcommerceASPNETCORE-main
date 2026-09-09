using ShoesEcommerce.Models.Interactions;
using DepartmentEntity = ShoesEcommerce.Models.Departments.Department;

namespace ShoesEcommerce.Models.Accounts
{
    public class Staff
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty; // Fixed typo

        public int DepartmentId { get; set; }

        // Navigation Properties
        public DepartmentEntity? Department { get; set; }
        public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
        public ICollection<QA> QAs { get; set; } = new List<QA>();

        // Helper Properties
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
