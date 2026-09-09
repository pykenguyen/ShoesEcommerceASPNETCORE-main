namespace ShoesEcommerce.Models
{

    // ErrorModel.cs dùng để in lỗi trên API từ Firebase    
    public class ErrorModel
    {
        public int code { get; set; }

        public string message { get; set; } 

        public string? details { get; set; }

        public List<ErrorModel> errors { get; set; }
    }
}
