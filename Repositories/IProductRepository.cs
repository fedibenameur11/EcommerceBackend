using EcommerceAPI.Models;

namespace EcommerceAPI.Repositories
{
    public interface IProductRepository
    {
        Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, int? categoryId);
        Task<Product?> GetByIdAsync(int id);
        Task AddAsync(Product product);
        void Update(Product product);
        void Remove(Product product);
        Task<int> SaveChangesAsync();
    }
}