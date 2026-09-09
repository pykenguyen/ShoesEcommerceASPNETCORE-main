namespace ShoesEcommerce.ViewModels.Payment
{
    public class VnPayReturnModel
    {
        // Các trường đã có
        public string? Vnp_ResponseCode { get; set; }
        public string? Vnp_TransactionNo { get; set; }
        public string? Vnp_OrderInfo { get; set; }

        // Các trường cần thêm (dựa trên lỗi CS1061 và CS0117)
        public string? Vnp_TxnRef { get; set; } // Dùng để xác định đơn hàng
        public string? Vnp_Amount { get; set; }
        public string? Vnp_SecureHash { get; set; }
        public bool IsSuccess { get; set; } // Dùng để xác định trạng thái thành công
    }
}