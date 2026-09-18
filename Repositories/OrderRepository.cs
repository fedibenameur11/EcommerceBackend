using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly EcommerceContext _db;
        public OrderRepository(EcommerceContext db) => _db = db;

        public async Task<Order?> GetByIdAsync(int id)
            => await _db.Orders
                        .Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
            => await _db.Orders.Include(o => o.Items).Where(o => o.UserId == userId).ToListAsync();

        public async Task<IEnumerable<Order>> GetAllAsync()
            => await _db.Orders.Include(o => o.Items).ToListAsync();

        public async Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.Orders.AsNoTracking();
            var totalCount = await query.CountAsync();
            var items = await query.Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, totalCount);
        }

        public async Task AddAsync(Order order)
        {
            await _db.Orders.AddAsync(order);
        }

        public void Update(Order order)
        {
            _db.Orders.Update(order);
        }

        public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}
