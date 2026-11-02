namespace EcommerceAPI.Models
{
    public enum CouponType
    {
        /// <summary>Remise en pourcentage du sous-total (Value entre 1 et 100).</summary>
        Percentage = 0,
        /// <summary>Remise d'un montant fixe en TND.</summary>
        FixedAmount = 1
    }

    public class Coupon
    {
        public int Id { get; set; }
        /// <summary>Code saisi par le client, toujours stocké en majuscules.</summary>
        public string Code { get; set; } = string.Empty;
        public CouponType Type { get; set; }
        public decimal Value { get; set; }
        /// <summary>Sous-total minimum pour pouvoir utiliser le code (0 = aucun).</summary>
        public decimal MinOrderAmount { get; set; }
        /// <summary>Nombre maximal d'utilisations (null = illimité).</summary>
        public int? MaxUses { get; set; }
        public int UsedCount { get; set; }
        public DateTime? ExpiresAt { get; set; }
        /// <summary>Si renseigné, la remise ne s'applique qu'aux produits de cette catégorie.</summary>
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
