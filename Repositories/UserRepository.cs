using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly EcommerceContext _db;
        public UserRepository(EcommerceContext db) => _db = db;

        public async Task<User?> GetByIdAsync(int id)
            => await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

        public async Task<User?> GetByEmailAsync(string email)
            => await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<IEnumerable<User>> GetAllAsync()
            => await _db.Users.AsNoTracking().ToListAsync();

        public async Task AddAsync(User user)
        {
            await _db.Users.AddAsync(user);
        }

        public void Update(User user)
        {
            _db.Users.Update(user);
        }

        public void Remove(User user) => _db.Users.Remove(user);

        public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}
