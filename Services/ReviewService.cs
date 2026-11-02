using EcommerceAPI.Data;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Services
{
    public interface IReviewService
    {
        Task<IEnumerable<ReviewDto>?> GetForProductAsync(int productId);
        /// <summary>Crée ou met à jour l'avis de l'utilisateur (un avis par produit). Null si le produit n'existe pas.</summary>
        Task<ReviewDto?> UpsertAsync(int productId, int userId, CreateReviewDto dto);
    }

    public class ReviewService : IReviewService
    {
        private readonly EcommerceContext _db;
        public ReviewService(EcommerceContext db) => _db = db;

        public async Task<IEnumerable<ReviewDto>?> GetForProductAsync(int productId)
        {
            if (!await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive)) return null;
            var reviews = await _db.Reviews.AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return reviews.Select(ToDto);
        }

        public async Task<ReviewDto?> UpsertAsync(int productId, int userId, CreateReviewDto dto)
        {
            if (dto.Rating is < 1 or > 5) throw new ArgumentException("La note doit être comprise entre 1 et 5.");
            var product = await _db.Products.AsNoTracking().Where(p => p.Id == productId && p.IsActive)
                .Select(p => new { p.SellerId }).FirstOrDefaultAsync();
            if (product == null) return null;
            if (product.SellerId == userId) throw new InvalidOperationException("Vous ne pouvez pas noter votre propre produit.");

            var review = await _db.Reviews.Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
            if (review == null)
            {
                review = new Review { ProductId = productId, UserId = userId };
                _db.Reviews.Add(review);
            }
            review.Rating = dto.Rating;
            review.Comment = (dto.Comment ?? string.Empty).Trim();
            review.CreatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _db.Entry(review).Reference(r => r.User).LoadAsync();
            return ToDto(review);
        }

        private static ReviewDto ToDto(Review r) => new()
        {
            Id = r.Id, ProductId = r.ProductId, UserId = r.UserId,
            UserName = r.User == null ? string.Empty : $"{r.User.FirstName} {r.User.LastName}".Trim(),
            Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt
        };
    }
}