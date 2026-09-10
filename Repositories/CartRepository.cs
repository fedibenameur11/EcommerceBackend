using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly EcommerceContext _db;
        public CartRepository(EcommerceContext db) => _db = db;

        public async Task<Cart?> GetByUserIdAsync(int userId)
            => await _db.Carts
                        .Include(c => c.Items)
                        .ThenInclude(ci => ci.Product)
                        .FirstOrDefaultAsync(c => c.UserId == userId);

        public async Task AddAsync(Cart cart) => await _db.Carts.AddAsync(cart);

        public void Update(Cart cart) => _db.Carts.Update(cart);

        public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}