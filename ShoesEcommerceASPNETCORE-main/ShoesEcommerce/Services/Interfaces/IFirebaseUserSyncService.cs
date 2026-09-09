namespace ShoesEcommerce.Services.Interfaces
{
    public interface IFirebaseUserSyncService
    {
        /// <summary>
        /// Đồng bộ tất cả người dùng từ Firebase Authentication vào cơ sở dữ liệu SQL Server.
        /// </summary>
        /// 
        Task SyncUsersAsync();

        /// <summary>
        /// Đồng bộ một người dùng cụ thể dựa trên Firebase UID.
        /// </summary>
        /// <param name="firebaseUid">Firebase UID của người dùng.</param>
        Task SyncUserAsync(string firebaseUid);
    }
}
