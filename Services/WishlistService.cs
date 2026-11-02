using EcommerceAPI.Data;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Services
{
    public interface IWishlistService
    {
        Task<IEnumerable<ProductDto>> GetAsync(int userId);
        /// <summary>Ajoute le produit (idempotent). False si le produit n'existe pas.</summary>
        Task<bool> AddAsync(int userId, int productId);
        Task<bool> RemoveAsync(int userId, int productId);
    }

    public class WishlistService : IWishlistService
    {
        private readonly EcommerceContext _db;
        private readonly IProductService _products;
        public WishlistService(EcommerceContext db, IProductService products)
        {
            _db = db;
            _products = products;
        }

        public async Task<IEnumerable<ProductDto>> GetAsync(int userId)
        {
            var ids = await _db.WishlistItems.AsNoTracking()
                .Where(w => w.UserId == userId && w.Product.IsActive)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => w.ProductId)
                .ToListAsync();
            var result = new List<ProductDto>();
            foreach (var id in ids)
            {
                var dto = await _products.GetByIdAsync(id);
                if (dto != null) result.Add(dto);
            }
            return result;
        }

        public async Task<bool> AddAsync(int userId, int productId)
        {
            if (!await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive)) return false;
            if (await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId)) return true;
            _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveAsync(int userId, int productId)
        {
            var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
            if (item == null) return false;
            _db.WishlistItems.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}