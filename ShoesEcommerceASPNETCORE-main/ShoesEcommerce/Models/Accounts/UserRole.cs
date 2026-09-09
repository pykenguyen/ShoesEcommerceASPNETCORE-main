namespace ShoesEcommerce.Models.Accounts
{
    public class UserRole
    {
        public int Id { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        public int? CustomerId { get; set; }
        public Customer Customer { get; set; }

        public int? StaffId { get; set; }
        public Staff Staff { get; set; }
    }

}
