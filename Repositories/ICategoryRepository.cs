using EcommerceAPI.Models;

namespace EcommerceAPI.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<Dictionary<int, int>> GetProductCountsAsync();
        Task AddAsync(Category category);
        void Update(Category category);
        void Remove(Category category);
        Task<int> SaveChangesAsync();
    }
}
