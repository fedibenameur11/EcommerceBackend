using System.Linq.Expressions;
using EcommerceAPI.Data;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Services
{
    public interface IReturnService
    {
        Task<ReturnRequestDto> CreateAsync(int userId, CreateReturnDto dto);
        Task<IEnumerable<ReturnRequestDto>> GetMineAsync(int userId);
        Task<IEnumerable<ReturnRequestDto>> GetAllAsync(ReturnStatus? status);
        Task<bool> UpdateStatusAsync(int id, UpdateReturnStatusDto dto);
    }

    public class ReturnService : IReturnService
    {
        /// <summary>Délai (en jours après la livraison) pour demander un retour.</summary>
        public const int ReturnWindowDays = 14;

        public static readonly string[] Reasons =
        {
            "Produit défectueux", "Produit non conforme à la description", "Mauvais article reçu",
            "Article endommagé à la livraison", "Ne convient pas", "Autre"
        };

        private readonly EcommerceContext _db;
        private readonly ILogger<ReturnService> _logger;
        public ReturnService(EcommerceContext db, ILogger<ReturnService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ReturnRequestDto> CreateAsync(int userId, CreateReturnDto dto)
        {
            if (!Reasons.Contains(dto.Reason)) throw new ArgumentException("Motif de retour invalide.");

            var item = await _db.OrderItems.Include(i => i.Order).FirstOrDefaultAsync(i => i.Id == dto.OrderItemId);
            if (item == null || item.Order.UserId != userId) throw new KeyNotFoundException("Article de commande introuvable.");
            if (item.Order.Status != OrderStatus.Delivered)
                throw new InvalidOperationException("Un retour n'est possible que pour une commande livrée.");
            if (item.Order.DeliveredDate.AddDays(ReturnWindowDays) < DateTime.UtcNow)
                throw new InvalidOperationException($"Le délai de retour de {ReturnWindowDays} jours est dépassé.");

            // Les demandes refusées ne comptent plus : l'article peut être redemandé
            var alreadyRequested = await _db.ReturnRequests
                .Where(r => r.OrderItemId == item.Id && r.Status != ReturnStatus.Rejected)
                .SumAsync(r => (int?)r.Quantity) ?? 0;
            if (dto.Quantity > item.Quantity - alreadyRequested)
                throw new InvalidOperationException(
                    $"Quantité trop élevée : {item.Quantity - alreadyRequested} article(s) peuvent encore être retournés.");

            var request = new ReturnRequest
            {
                OrderId = item.OrderId, OrderItemId = item.Id, UserId = userId,
                Quantity = dto.Quantity, Reason = dto.Reason, Details = (dto.Details ?? string.Empty).Trim()
            };
            _db.ReturnRequests.Add(request);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Retour {ReturnId} demandé par l'utilisateur {UserId} pour la commande {OrderId}", request.Id, userId, item.OrderId);
            return await Query().Where(r => r.Id == request.Id).Select(Project).FirstAsync();
        }

        public async Task<IEnumerable<ReturnRequestDto>> GetMineAsync(int userId)
            => await Query().Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).Select(Project).ToListAsync();

        public async Task<IEnumerable<ReturnRequestDto>> GetAllAsync(ReturnStatus? status)
        {
            var query = Query();
            if (status.HasValue) query = query.Where(r => r.Status == status.Value);
            return await query.OrderByDescending(r => r.CreatedAt).Select(Project).ToListAsync();
        }

        public async Task<bool> UpdateStatusAsync(int id, UpdateReturnStatusDto dto)
        {
            var r = await _db.ReturnRequests
                .Include(x => x.OrderItem)
                .Include(x => x.Order).ThenInclude(o => o.Items)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return false;

            var allowed = r.Status switch
            {
                ReturnStatus.Requested => new[] { ReturnStatus.Approved, ReturnStatus.Rejected },
                ReturnStatus.Approved => new[] { ReturnStatus.Received },
                ReturnStatus.Received => new[] { ReturnStatus.Refunded },
                _ => Array.Empty<ReturnStatus>()
            };
            if (!allowed.Contains(dto.Status))
                throw new InvalidOperationException("Ce changement de statut n'est pas autorisé.");

            if (dto.Status == ReturnStatus.Received)
            {
                // L'article revient en rayon
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == r.OrderItem.ProductId);
                if (product != null) product.StockQuantity += r.Quantity;
            }
            if (dto.Status == ReturnStatus.Refunded)
                r.RefundAmount = ComputeRefund(r);

            r.Status = dto.Status;
            if (!string.IsNullOrWhiteSpace(dto.AdminNote)) r.AdminNote = dto.AdminNote.Trim();
            r.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Retour {ReturnId} : statut -> {Status}", id, dto.Status);
            return true;
        }

        /// <summary>Prix payé pour les articles retournés, diminué de leur part de la remise du code promo.</summary>
        private static decimal ComputeRefund(ReturnRequest r)
        {
            var gross = r.OrderItem.UnitPrice * r.Quantity;
            var subtotal = r.Order.Items.Sum(i => i.TotalPrice);
            var discountShare = subtotal > 0 ? r.Order.DiscountAmount * gross / subtotal : 0m;
            return Math.Round(Math.Max(gross - discountShare, 0m), 3);
        }

        private IQueryable<ReturnRequest> Query() => _db.ReturnRequests.AsNoTracking();

        private static readonly Expression<Func<ReturnRequest, ReturnRequestDto>> Project = r => new ReturnRequestDto
        {
            Id = r.Id, OrderId = r.OrderId, OrderNumber = r.Order.OrderNumber, OrderItemId = r.OrderItemId,
            ProductId = r.OrderItem.ProductId, ProductName = r.OrderItem.Product.Name, ProductImageUrl = r.OrderItem.Product.ImageUrl,
            CustomerName = r.User.FirstName + " " + r.User.LastName, CustomerEmail = r.User.Email,
            Quantity = r.Quantity, Reason = r.Reason, Details = r.Details, Status = r.Status,
            AdminNote = r.AdminNote, RefundAmount = r.RefundAmount, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt
        };
    }
}
