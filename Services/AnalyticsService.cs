using EcommerceAPI.Data;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Services
{
    public interface IAnalyticsService
    {
        /// <param name="sellerId">Si renseigné, les chiffres ne portent que sur les produits de ce vendeur.</param>
        Task<AnalyticsDto> GetAsync(int days, int topCount, int lowStockThreshold, int? sellerId = null);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly EcommerceContext _db;
        public AnalyticsService(EcommerceContext db) => _db = db;

        public async Task<AnalyticsDto> GetAsync(int days, int topCount, int lowStockThreshold, int? sellerId = null)
        {
            var since = DateTime.UtcNow.Date.AddDays(-(days - 1));
            if (sellerId.HasValue) return await GetForSellerAsync(sellerId.Value, since, days, topCount, lowStockThreshold);

            // Les commandes annulées ne comptent pas dans le chiffre d'affaires.
            var orders = await _db.Orders.AsNoTracking()
                .Where(o => o.OrderDate >= since && o.Status != OrderStatus.Cancelled)
                .Select(o => new { o.OrderDate, o.DeliveryFee, o.DiscountAmount, Items = o.Items.Sum(i => i.TotalPrice) })
                .ToListAsync();

            var byDay = orders.GroupBy(o => o.OrderDate.Date)
                .ToDictionary(g => g.Key, g => (Revenue: g.Sum(o => o.Items + o.DeliveryFee - o.DiscountAmount), Count: g.Count()));

            var series = Enumerable.Range(0, days)
                .Select(i => since.AddDays(i))
                .Select(d => new RevenuePointDto
                {
                    Date = d,
                    Revenue = byDay.TryGetValue(d, out var v) ? v.Revenue : 0,
                    Orders = byDay.TryGetValue(d, out var v2) ? v2.Count : 0
                }).ToList();

            var top = await _db.OrderItems.AsNoTracking()
                .Where(i => i.Order.OrderDate >= since && i.Order.Status != OrderStatus.Cancelled)
                .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.ImageUrl })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId, Name = g.Key.Name, ImageUrl = g.Key.ImageUrl,
                    QuantitySold = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.TotalPrice)
                })
                .OrderByDescending(t => t.QuantitySold).ThenByDescending(t => t.Revenue)
                .Take(topCount)
                .ToListAsync();

            var low = await _db.Products.AsNoTracking()
                .Where(p => p.IsActive && p.StockQuantity <= lowStockThreshold)
                .OrderBy(p => p.StockQuantity).ThenBy(p => p.Name)
                .Select(p => new LowStockProductDto
                {
                    ProductId = p.Id, Name = p.Name, ImageUrl = p.ImageUrl, StockQuantity = p.StockQuantity
                })
                .ToListAsync();

            return new AnalyticsDto
            {
                Days = days,
                TotalRevenue = series.Sum(s => s.Revenue),
                TotalOrders = series.Sum(s => s.Orders),
                ItemsSold = await _db.OrderItems.Where(i => i.Order.OrderDate >= since && i.Order.Status != OrderStatus.Cancelled).SumAsync(i => (int?)i.Quantity) ?? 0,
                RevenueOverTime = series,
                TopProducts = top,
                LowStock = low,
                LowStockThreshold = lowStockThreshold
            };
        }

        /// <summary>Chiffre d'affaires d'un vendeur : uniquement ses articles (hors livraison et remises globales).</summary>
        private async Task<AnalyticsDto> GetForSellerAsync(int sellerId, DateTime since, int days, int topCount, int lowStockThreshold)
        {
            var lines = await _db.OrderItems.AsNoTracking()
                .Where(i => i.Order.OrderDate >= since && i.Order.Status != OrderStatus.Cancelled && i.Product.SellerId == sellerId)
                .Select(i => new { i.OrderId, i.Order.OrderDate, i.TotalPrice, i.Quantity })
                .ToListAsync();

            var byDay = lines.GroupBy(l => l.OrderDate.Date)
                .ToDictionary(g => g.Key, g => (Revenue: g.Sum(l => l.TotalPrice), Count: g.Select(l => l.OrderId).Distinct().Count()));
            var series = Enumerable.Range(0, days)
                .Select(i => since.AddDays(i))
                .Select(d => new RevenuePointDto
                {
                    Date = d,
                    Revenue = byDay.TryGetValue(d, out var v) ? v.Revenue : 0,
                    Orders = byDay.TryGetValue(d, out var v2) ? v2.Count : 0
                }).ToList();

            var top = await _db.OrderItems.AsNoTracking()
                .Where(i => i.Order.OrderDate >= since && i.Order.Status != OrderStatus.Cancelled && i.Product.SellerId == sellerId)
                .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.ImageUrl })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId, Name = g.Key.Name, ImageUrl = g.Key.ImageUrl,
                    QuantitySold = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.TotalPrice)
                })
                .OrderByDescending(t => t.QuantitySold).ThenByDescending(t => t.Revenue)
                .Take(topCount)
                .ToListAsync();

            var low = await _db.Products.AsNoTracking()
                .Where(p => p.IsActive && p.SellerId == sellerId && p.StockQuantity <= lowStockThreshold)
                .OrderBy(p => p.StockQuantity).ThenBy(p => p.Name)
                .Select(p => new LowStockProductDto
                {
                    ProductId = p.Id, Name = p.Name, ImageUrl = p.ImageUrl, StockQuantity = p.StockQuantity
                })
                .ToListAsync();

            return new AnalyticsDto
            {
                Days = days,
                TotalRevenue = series.Sum(s => s.Revenue),
                TotalOrders = lines.Select(l => l.OrderId).Distinct().Count(),
                ItemsSold = lines.Sum(l => l.Quantity),
                RevenueOverTime = series,
                TopProducts = top,
                LowStock = low,
                LowStockThreshold = lowStockThreshold
            };
        }
    }
}