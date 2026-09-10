using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class CreateOrderDto
    {
        [Required, MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required, MinLength(1)]
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
