using System;
using System.ComponentModel.DataAnnotations;

namespace ShoesEcommerce.ViewModels.Product
{
    public class ProductCommentViewModel
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Nội dung bình luận không được để trống.")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Nội dung bình luận phải từ 2 đến 255 ký tự.")]
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductQAViewModel
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Câu hỏi không được để trống.")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Câu hỏi phải từ 2 đến 255 ký tự.")]
        public string Question { get; set; }
        public string Answer { get; set; }
        public DateTime AskedAt { get; set; }
        public DateTime? AnsweredAt { get; set; }
    }
}
