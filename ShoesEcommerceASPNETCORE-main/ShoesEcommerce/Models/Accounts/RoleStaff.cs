namespace ShoesEcommerce.Models.Accounts
{
    public class RoleStaff
    {
        public int StaffId { get; set; }
        public Staff Staff { get; set; }

        public int RoleId { get; set; }

        public Role Role { get; set; } 
    }
}
