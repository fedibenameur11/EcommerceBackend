using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class CreateOrderDto
    {
        [Required, MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        /// <summary>Code promo facultatif ; la remise est recalculée par le serveur.</summary>
        [MaxLength(32)]
        public string? CouponCode { get; set; }

        [Required, MinLength(1)]
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        /// <summary>Pointure / taille choisie ; obligatoire si le produit en propose.</summary>
        [MaxLength(10)]
        public string? Size { get; set; }
    }
}
