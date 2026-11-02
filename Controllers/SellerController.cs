using System.Security.Claims;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Controllers
{
    /// <summary>Espace vendeur : statistiques et commandes portant sur ses propres produits.</summary>
    [ApiController]
    [Route("api/seller")]
    [Authorize(Roles = "Seller")]
    public class SellerController : ControllerBase
    {
        private readonly IAnalyticsService _analytics;
        private readonly EcommerceContext _db;

        public SellerController(IAnalyticsService analytics, EcommerceContext db)
        {
            _analytics = analytics;
            _db = db;
        }

        [HttpGet("analytics")]
        public async Task<ActionResult<AnalyticsDto>> Analytics(
            [FromQuery] int days = 30, [FromQuery] int top = 5, [FromQuery] int lowStockThreshold = 5)
        {
            if (days is < 1 or > 365) return BadRequest("days doit être compris entre 1 et 365.");
            if (top is < 1 or > 50) return BadRequest("top doit être compris entre 1 et 50.");
            if (lowStockThreshold < 0) return BadRequest("lowStockThreshold doit être positif.");
            return Ok(await _analytics.GetAsync(days, top, lowStockThreshold, CurrentUserId()));
        }

        /// <summary>Commandes contenant au moins un produit du vendeur (montant = uniquement ses articles).</summary>
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<SellerOrderDto>>> Orders()
        {
            var sellerId = CurrentUserId();
            var orders = await _db.Orders.AsNoTracking()
                .Where(o => o.Items.Any(i => i.Product.SellerId == sellerId))
                .OrderByDescending(o => o.OrderDate)
                .Take(200)
                .Select(o => new SellerOrderDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    CustomerName = o.User.FirstName + " " + o.User.LastName,
                    ShippingAddress = o.ShippingAddress,
                    ItemCount = o.Items.Where(i => i.Product.SellerId == sellerId).Sum(i => i.Quantity),
                    Total = o.Items.Where(i => i.Product.SellerId == sellerId).Sum(i => i.TotalPrice)
                })
                .ToListAsync();
            return Ok(orders);
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public class SellerOrderDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
    }
}
