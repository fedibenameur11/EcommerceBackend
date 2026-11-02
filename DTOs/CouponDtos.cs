using System.ComponentModel.DataAnnotations;
using EcommerceAPI.Models;

namespace EcommerceAPI.DTOs
{
    /// <summary>Coupon tel qu'affiché dans l'administration.</summary>
    public class CouponDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public CouponType Type { get; set; }
        public decimal Value { get; set; }
        public decimal MinOrderAmount { get; set; }
        public int? MaxUses { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int UsedCount { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveCouponDto
    {
        [Required, StringLength(32, MinimumLength = 3)]
        [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Le code ne peut contenir que des lettres, chiffres, « - » et « _ ».")]
        public string Code { get; set; } = string.Empty;

        public CouponType Type { get; set; }

        [Range(0.001, 1_000_000)]
        public decimal Value { get; set; }

        [Range(0, 1_000_000)]
        public decimal MinOrderAmount { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxUses { get; set; }

        public DateTime? ExpiresAt { get; set; }
        /// <summary>Catégorie ciblée (null = tout le catalogue).</summary>
        public int? CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ValidateCouponDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Range(0, 10_000_000)]
        public decimal Subtotal { get; set; }

        /// <summary>Lignes du panier : permet de calculer la remise d'un coupon limité à une catégorie.</summary>
        public List<CreateOrderItemDto>? Items { get; set; }
    }

    /// <summary>Aperçu de la remise pour un panier, renvoyé au client.</summary>
    public class CouponValidationDto
    {
        public string Code { get; set; } = string.Empty;
        public CouponType Type { get; set; }
        public decimal Value { get; set; }
        /// <summary>Montant de la remise en TND, déjà plafonné au sous-total.</summary>
        public decimal Discount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
