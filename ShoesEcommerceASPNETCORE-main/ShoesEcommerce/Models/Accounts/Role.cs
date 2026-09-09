namespace ShoesEcommerce.Models.Accounts
{

    public enum UserType
    {
        Customer,
        Staff
    }
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public UserType UserType { get; set; } // NEW 👈

        public ICollection<RolePermission> RolePermissions { get; set; }
    }
}
