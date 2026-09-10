using EcommerceAPI.Models;

namespace EcommerceAPI.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Order>> GetAllAsync();
        Task AddAsync(Order order);
        void Update(Order order);
        Task<int> SaveChangesAsync();
    }
}
