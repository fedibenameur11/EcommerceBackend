using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly EcommerceContext _db;
        public CategoryRepository(EcommerceContext db) => _db = db;

        public async Task<IEnumerable<Category>> GetAllAsync()
            => await _db.Categories.AsNoTracking().ToListAsync();

        public async Task<Category?> GetByIdAsync(int id)
            => await _db.Categories.FindAsync(id);

        public async Task AddAsync(Category category)
        {
            await _db.Categories.AddAsync(category);
        }

        public void Update(Category category)
        {
            _db.Categories.Update(category);
        }

        public void Remove(Category category)
        {
            _db.Categories.Remove(category);
        }

        public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}
