using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Accounts;
using System.Runtime.InteropServices;




namespace ShoesEcommerce.Services
{
    public class FirebaseUserSyncService
    {
        private readonly AppDbContext _dbcontext; 

        public FirebaseUserSyncService(AppDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        //public async Task SyncUserAsync(string firebaseUid)
        //{
        //    var firebaseUser = await FirebaseAuth.DefaultInstance.GetUserAsync(firebaseUid);
        //    var customerRole = await EnsureCustomerRoleExistsAsync();

        //    var dbCustomer = await _dbcontext.Customers
        //        .Include(c => c.UserRoles)
        //        .FirstOrDefaultAsync(c => c.FirebaseUid == firebaseUid);

        //    if (dbCustomer == null)
        //    {
        //        dbCustomer = new Customer
        //        {
        //            FirebaseUid = firebaseUser.Uid,
        //            Email = firebaseUser.Email,
        //            // Ánh xạ DisplayName thành FirstName + LastName (nếu có)
        //            FirstName = firebaseUser.DisplayName?.Split(' ').FirstOrDefault() ?? "Unknown",
        //            LastName = firebaseUser.DisplayName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
        //            PhoneNumber = firebaseUser.PhoneNumber ?? "", // Firebase có thể cung cấp PhoneNumber
        //            DateOfBirth = DateTime.MinValue, // Mặc định, cần người dùng cập nhật sau
        //            ImageUrl = firebaseUser.PhotoUrl,
        //            Address = "",
        //            City = "",
        //            State = "",
        //            CreatedAt = DateTime.Now,
        //            UpdatedAt = DateTime.Now
        //        };
        //        _dbcontext.Customers.Add(dbCustomer);
        //        _dbcontext.UserRoles.Add(new UserRole { Customer = dbCustomer, Role = customerRole });
        //    }
        //    else
        //    {
        //        // Cập nhật thông tin
        //        dbCustomer.Email = firebaseUser.Email;
        //        dbCustomer.FirstName = firebaseUser.DisplayName?.Split(' ').FirstOrDefault() ?? dbCustomer.FirstName;
        //        dbCustomer.LastName = firebaseUser.DisplayName?.Split(' ').Skip(1).FirstOrDefault() ?? dbCustomer.LastName;
        //        dbCustomer.PhoneNumber = firebaseUser.PhoneNumber ?? dbCustomer.PhoneNumber;
        //        dbCustomer.ImageUrl = firebaseUser.PhotoUrl ?? dbCustomer.ImageUrl;
        //        dbCustomer.UpdatedAt = DateTime.Now;
        //    }

        //    await _dbcontext.SaveChangesAsync();
        //}


        private async Task<Role> EnsureCustomerRoleExistsAsync()
        {
            var role = await _dbcontext.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (role == null)
            {
                role = new Role { Name = "Customer" };
                _dbcontext.Roles.Add(role);
                await _dbcontext.SaveChangesAsync();
            }
            return role;
        }
    }
}
