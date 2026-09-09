using ShoesEcommerce.Models.Interactions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface ICommentRepository
    {
        Task<List<Comment>> GetCommentsByProductIdAsync(int productId);
        Task AddCommentAsync(Comment comment);
    }

    public interface IQARepository
    {
        Task<List<QA>> GetQAsByProductIdAsync(int productId);
        Task AddQAAsync(QA qa);
    }
}