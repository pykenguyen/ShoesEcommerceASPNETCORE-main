using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.ViewModels.Product;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoesEcommerce.Services
{
    public interface ICommentService
    {
        Task<List<ProductCommentViewModel>> GetCommentsAsync(int productId);
        Task AddCommentAsync(ProductCommentViewModel model);
        Task<List<ProductQAViewModel>> GetQAsAsync(int productId);
        Task AddQAAsync(ProductQAViewModel model);
    }

    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepo;
        private readonly IQARepository _qaRepo;
        public CommentService(ICommentRepository commentRepo, IQARepository qaRepo)
        {
            _commentRepo = commentRepo;
            _qaRepo = qaRepo;
        }
        public async Task<List<ProductCommentViewModel>> GetCommentsAsync(int productId)
        {
            var comments = await _commentRepo.GetCommentsByProductIdAsync(productId);
            return comments.Select(c => new ProductCommentViewModel
            {
                Id = c.Id,
                CustomerId = c.CustomerId,
                CustomerName = c.Customer?.FullName ?? "Khách hàng",
                ProductId = c.ProductId,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            }).ToList();
        }
        public async Task AddCommentAsync(ProductCommentViewModel model)
        {
            var comment = new Comment
            {
                CustomerId = model.CustomerId,
                ProductId = model.ProductId,
                Content = model.Content,
                CreatedAt = DateTime.Now
            };
            await _commentRepo.AddCommentAsync(comment);
        }
        public async Task<List<ProductQAViewModel>> GetQAsAsync(int productId)
        {
            var qas = await _qaRepo.GetQAsByProductIdAsync(productId);
            return qas.Select(q => new ProductQAViewModel
            {
                Id = q.Id,
                CustomerId = q.CustomerId,
                CustomerName = q.Customer?.FullName ?? "Khách hàng",
                ProductId = q.ProductId,
                Question = q.Question,
                Answer = q.Answer,
                AskedAt = q.AskedAt,
                AnsweredAt = q.AnsweredAt
            }).ToList();
        }
        public async Task AddQAAsync(ProductQAViewModel model)
        {
            var qa = new QA
            {
                CustomerId = model.CustomerId,
                ProductId = model.ProductId,
                Question = model.Question,
                Answer = model.Answer,
                AskedAt = DateTime.Now,
                AnsweredAt = model.AnsweredAt
            };
            await _qaRepo.AddQAAsync(qa);
        }
    }
}
