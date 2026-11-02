using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly EcommerceContext _db;
        public ProductRepository(EcommerceContext db) => _db = db;

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(ProductQuery q)
        {
            var now = DateTime.UtcNow;
            var query = _db.Products.AsNoTracking().Where(p => p.IsActive);
            if (q.OnSale == true)
                query = query.Where(p => p.SalePrice != null && p.SaleEndsAt > now
                    && (p.SaleStartsAt == null || p.SaleStartsAt <= now) && p.SalePrice < p.Price);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var pattern = LikePattern(q.Search);
                query = query.Where(p => EF.Functions.ILike(p.Name, pattern, "\\")
                    || EF.Functions.ILike(p.Brand, pattern, "\\")
                    || EF.Functions.ILike(p.Description, pattern, "\\"));
            }
            if (q.CategoryId.HasValue) query = query.Where(p => p.CategoryId == q.CategoryId.Value);
            if (q.SellerId.HasValue) query = query.Where(p => p.SellerId == q.SellerId.Value);
            if (q.MinPrice.HasValue) query = query.Where(p => (p.SalePrice != null && p.SaleEndsAt > now && (p.SaleStartsAt == null || p.SaleStartsAt <= now) && p.SalePrice < p.Price ? p.SalePrice.Value : p.Price) >= q.MinPrice.Value);
            if (q.MaxPrice.HasValue) query = query.Where(p => (p.SalePrice != null && p.SaleEndsAt > now && (p.SaleStartsAt == null || p.SaleStartsAt <= now) && p.SalePrice < p.Price ? p.SalePrice.Value : p.Price) <= q.MaxPrice.Value);
            if (!string.IsNullOrWhiteSpace(q.Brand))
            {
                var brand = q.Brand.Trim();
                query = query.Where(p => p.Brand == brand);
            }
            if (!string.IsNullOrWhiteSpace(q.Gender))
            {
                var gender = q.Gender.Trim();
                query = query.Where(p => p.Gender == gender || p.Gender == "");
            }
            if (!string.IsNullOrWhiteSpace(q.Color))
            {
                var color = q.Color.Trim();
                query = query.Where(p => p.Color == color);
            }
            if (!string.IsNullOrWhiteSpace(q.Size))
            {
                // Sizes = "38,39,40" : on cherche la valeur entre virgules pour qu'"S" ne corresponde pas à "XS"
                var token = "," + q.Size.Trim() + ",";
                query = query.Where(p => ("," + p.Sizes + ",").Contains(token));
            }
            if (q.MinRating.HasValue)
            {
                var min = (double)q.MinRating.Value;
                query = query.Where(p => _db.Reviews.Where(r => r.ProductId == p.Id).Average(r => (double?)r.Rating) >= min);
            }

            // Le tri se termine toujours par l'Id pour que la pagination reste stable
            var ordered = q.Sort switch
            {
                "price-asc" => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
                "price-desc" => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
                "name" => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
                "newest" => query.OrderByDescending(p => p.Id),
                _ => query.OrderBy(p => p.Id)
            };

            var totalCount = await query.CountAsync();
            var items = await ordered
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .ToListAsync();
            return (items, totalCount);
        }

        public async Task<List<Product>> GetSuggestionsAsync(string term, int take)
        {
            var contains = LikePattern(term);
            var prefix = contains.Substring(1); // sans le « % » initial : « commence par »
            return await _db.Products.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive && (EF.Functions.ILike(p.Name, contains, "\\") || EF.Functions.ILike(p.Brand, contains, "\\")))
                .OrderBy(p => EF.Functions.ILike(p.Name, prefix, "\\") ? 0 : 1)
                .ThenBy(p => p.Name)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<string>> GetBrandsAsync()
            => await _db.Products.AsNoTracking()
                .Where(p => p.IsActive && p.Brand != "")
                .Select(p => p.Brand).Distinct().OrderBy(b => b).ToListAsync();

        /// <summary>Motif ILIKE « contient » avec échappement des caractères spéciaux saisis.</summary>
        private static string LikePattern(string term)
        {
            var escaped = term.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            return $"%{escaped}%";
        }

        public async Task<Product?> GetByIdAsync(int id)
            => await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        public async Task<Product?> GetDetailedByIdAsync(int id)
            => await _db.Products.AsNoTracking()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        public async Task<Product?> GetWithImagesAsync(int id)
            => await _db.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        public async Task<Dictionary<int, (double Average, int Count)>> GetRatingSummariesAsync(IEnumerable<int> productIds)
        {
            var ids = productIds.Distinct().ToList();
            var rows = await _db.Reviews.AsNoTracking()
                .Where(r => ids.Contains(r.ProductId))
                .GroupBy(r => r.ProductId)
                .Select(g => new { ProductId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToListAsync();
            return rows.ToDictionary(r => r.ProductId, r => (Math.Round(r.Average, 1), r.Count));
        }

        public async Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var list = ids.Distinct().ToList();
            return await _db.Products.AsNoTracking()
                .Where(p => p.IsActive && list.Contains(p.Id)).ToListAsync();
        }

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
