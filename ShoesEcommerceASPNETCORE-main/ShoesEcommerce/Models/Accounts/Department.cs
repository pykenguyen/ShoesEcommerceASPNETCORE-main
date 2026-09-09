namespace ShoesEcommerce.Models.Accounts
{
    public class Department
    {
        
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Staff> Staffs { get; set; }
    }
}
