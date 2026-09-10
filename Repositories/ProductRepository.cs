using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly EcommerceContext _db;
        public ProductRepository(EcommerceContext db) => _db = db;

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, int? categoryId)
        {
            var query = _db.Products.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.ToLower().Contains(search.ToLower())
                    || p.Description.ToLower().Contains(search.ToLower()));
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var totalCount = await query.CountAsync();
            var items = await query.OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, totalCount);
        }

        public async Task<Product?> GetByIdAsync(int id)
            => await _db.Products.FindAsync(id);

        public async Task AddAsync(Product product)
        {
            await _db.Products.AddAsync(product);
        }

        public void Update(Product product)
        {
            _db.Products.Update(product);
        }

        public void Remove(Product product)
        {
            _db.Products.Remove(product);
        }

        public async Task<int> SaveChangesAsync()
            => await _db.SaveChangesAsync();
    }
}