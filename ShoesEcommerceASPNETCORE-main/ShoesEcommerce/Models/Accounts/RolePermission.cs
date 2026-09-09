using System.Security.Permissions;

namespace ShoesEcommerce.Models.Accounts
{
    public class RolePermission
    {
        public int RoleId { get; set;}
        public Role Role { get; set; }

        public int PermissionId { get; set; }
        public Permission Permission { get; set; }  


    }
}
