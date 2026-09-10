using EcommerceAPI.Models;

namespace EcommerceAPI.Repositories
{
    public interface ICartRepository
    {
        Task<Cart?> GetByUserIdAsync(int userId);
        Task AddAsync(Cart cart);
        void Update(Cart cart);
        Task<int> SaveChangesAsync();
    }
}