using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoesEcommerce.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly AppDbContext _context;
        public CommentRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Comment>> GetCommentsByProductIdAsync(int productId)
        {
            return await _context.Comments.Include(c => c.Customer).Where(c => c.ProductId == productId).OrderByDescending(c => c.CreatedAt).ToListAsync();
        }
        public async Task AddCommentAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
        }
    }

    public class QARepository : IQARepository
    {
        private readonly AppDbContext _context;
        public QARepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<QA>> GetQAsByProductIdAsync(int productId)
        {
            return await _context.QAs.Include(q => q.Customer).Where(q => q.ProductId == productId).OrderByDescending(q => q.AskedAt).ToListAsync();
        }
        public async Task AddQAAsync(QA qa)
        {
            _context.QAs.Add(qa);
            await _context.SaveChangesAsync();
        }
    }
}