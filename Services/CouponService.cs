using EcommerceAPI.Data;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Services
{
    public interface ICouponService
    {
        Task<IEnumerable<CouponDto>> GetAllAsync();
        Task<CouponDto> CreateAsync(SaveCouponDto dto);
        Task<bool> UpdateAsync(int id, SaveCouponDto dto);
        Task<bool> DeleteAsync(int id);
        /// <summary>Aperçu pour le client : lève ArgumentException (400) avec un message clair si le code est refusé.</summary>
        Task<CouponValidationDto> ValidateAsync(string code, decimal subtotal, IReadOnlyCollection<CreateOrderItemDto>? items = null);
        /// <summary>Même contrôle, et renvoie le coupon pour que la commande en consomme une utilisation.</summary>
        Task<(Coupon Coupon, decimal Discount)> ResolveAsync(string code, decimal subtotal, IReadOnlyCollection<(int CategoryId, decimal Total)>? lines = null);
        /// <summary>Consomme une utilisation de façon atomique (échoue si la limite vient d'être atteinte).</summary>
        Task ConsumeAsync(Coupon coupon);
        /// <summary>Rend une utilisation (annulation de commande).</summary>
        Task ReleaseAsync(string code);
    }

    public class CouponService : ICouponService
    {
        private readonly EcommerceContext _db;
        public CouponService(EcommerceContext db) => _db = db;

        public static string Normalize(string code) => code.Trim().ToUpperInvariant();

        public async Task<IEnumerable<CouponDto>> GetAllAsync()
        {
            var coupons = await _db.Coupons.AsNoTracking().Include(c => c.Category).OrderByDescending(c => c.CreatedAt).ToListAsync();
            return coupons.Select(ToDto);
        }

        public async Task<CouponDto> CreateAsync(SaveCouponDto dto)
        {
            await ValidateAsync(dto);
            var code = Normalize(dto.Code);
            if (await _db.Coupons.AnyAsync(c => c.Code == code))
                throw new InvalidOperationException($"Le code « {code} » existe déjà.");

            var coupon = new Coupon { Code = code };
            Apply(coupon, dto);
            _db.Coupons.Add(coupon);
            await _db.SaveChangesAsync();
            await _db.Entry(coupon).Reference(c => c.Category).LoadAsync();
            return ToDto(coupon);
        }

        public async Task<bool> UpdateAsync(int id, SaveCouponDto dto)
        {
            await ValidateAsync(dto);
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id);
            if (coupon == null) return false;

            var code = Normalize(dto.Code);
            if (await _db.Coupons.AnyAsync(c => c.Code == code && c.Id != id))
                throw new InvalidOperationException($"Le code « {code} » existe déjà.");

            coupon.Code = code;
            Apply(coupon, dto);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id);
            if (coupon == null) return false;
            _db.Coupons.Remove(coupon);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<CouponValidationDto> ValidateAsync(string code, decimal subtotal, IReadOnlyCollection<CreateOrderItemDto>? items = null)
        {
            IReadOnlyCollection<(int CategoryId, decimal Total)>? lines = null;
            if (items is { Count: > 0 })
            {
                // Les prix viennent de la base (vente flash comprise), jamais du client
                var ids = items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _db.Products.AsNoTracking().Where(p => p.IsActive && ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
                var now = DateTime.UtcNow;
                lines = items.Where(i => products.ContainsKey(i.ProductId))
                    .Select(i => (products[i.ProductId].CategoryId, products[i.ProductId].CurrentPrice(now) * i.Quantity))
                    .ToList();
                subtotal = lines.Sum(l => l.Total);
            }
            var (coupon, discount) = await ResolveAsync(code, subtotal, lines);
            return new CouponValidationDto
            {
                Code = coupon.Code,
                Type = coupon.Type,
                Value = coupon.Value,
                Discount = discount,
                Message = coupon.Type == CouponType.Percentage
                    ? $"Code appliqué : -{coupon.Value:0.##} %"
                    : $"Code appliqué : -{coupon.Value:0.###} TND"
            };
        }

        public async Task<(Coupon Coupon, decimal Discount)> ResolveAsync(string code, decimal subtotal, IReadOnlyCollection<(int CategoryId, decimal Total)>? lines = null)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Saisissez un code promo.");
            var normalized = Normalize(code);
            var coupon = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized);

            // Message volontairement identique pour « inconnu » et « désactivé » : on ne révèle pas les codes existants
            if (coupon == null || !coupon.IsActive) throw new ArgumentException("Ce code promo n'est pas valide.");
            if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value <= DateTime.UtcNow)
                throw new ArgumentException("Ce code promo a expiré.");
            if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
                throw new ArgumentException("Ce code promo a atteint son nombre maximal d'utilisations.");
            if (subtotal < coupon.MinOrderAmount)
                throw new ArgumentException($"Ce code nécessite un panier d'au moins {coupon.MinOrderAmount:0.###} TND.");

            // Coupon limité à une catégorie : la remise ne porte que sur les articles de cette catégorie
            var eligible = subtotal;
            if (coupon.CategoryId.HasValue)
            {
                eligible = lines?.Where(l => l.CategoryId == coupon.CategoryId.Value).Sum(l => l.Total) ?? 0m;
                if (eligible <= 0)
                {
                    var name = await _db.Categories.AsNoTracking().Where(c => c.Id == coupon.CategoryId.Value).Select(c => c.Name).FirstOrDefaultAsync();
                    throw new ArgumentException($"Ce code ne s'applique qu'aux produits de la catégorie « {name} ».");
                }
            }

            var raw = coupon.Type == CouponType.Percentage
                ? eligible * coupon.Value / 100m
                : coupon.Value;
            // La remise ne peut jamais dépasser le montant éligible (pas de total négatif)
            var discount = Math.Min(Math.Round(raw, 3), eligible);
            return (coupon, discount);
        }

        public async Task ConsumeAsync(Coupon coupon)
        {
            // UPDATE conditionnel : deux commandes simultanées ne peuvent pas dépasser MaxUses
            var rows = await _db.Coupons
                .Where(c => c.Id == coupon.Id && (c.MaxUses == null || c.UsedCount < c.MaxUses))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount + 1));
            if (rows == 0)
                throw new InvalidOperationException("Ce code promo a atteint son nombre maximal d'utilisations.");
        }

        public async Task ReleaseAsync(string code)
        {
            var normalized = Normalize(code);
            await _db.Coupons
                .Where(c => c.Code == normalized && c.UsedCount > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount - 1));
        }

        private async Task ValidateAsync(SaveCouponDto dto)
        {
            if (dto.Type == CouponType.Percentage && dto.Value > 100)
                throw new ArgumentException("Un pourcentage ne peut pas dépasser 100.");
            if (dto.CategoryId.HasValue && !await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value))
                throw new ArgumentException("La catégorie choisie n'existe pas.");
        }

        private static void Apply(Coupon coupon, SaveCouponDto dto)
        {
            coupon.Type = dto.Type;
            coupon.Value = dto.Value;
            coupon.MinOrderAmount = dto.MinOrderAmount;
            coupon.MaxUses = dto.MaxUses;
            coupon.CategoryId = dto.CategoryId;
            coupon.ExpiresAt = dto.ExpiresAt?.ToUniversalTime();
            coupon.IsActive = dto.IsActive;
        }

        private static CouponDto ToDto(Coupon c) => new()
        {
            Id = c.Id, Code = c.Code, Type = c.Type, Value = c.Value,
            MinOrderAmount = c.MinOrderAmount, MaxUses = c.MaxUses, UsedCount = c.UsedCount,
            CategoryId = c.CategoryId, CategoryName = c.Category?.Name,
            ExpiresAt = c.ExpiresAt, IsActive = c.IsActive, CreatedAt = c.CreatedAt,
            IsExpired = c.ExpiresAt.HasValue && c.ExpiresAt.Value <= DateTime.UtcNow
        };
    }
}
