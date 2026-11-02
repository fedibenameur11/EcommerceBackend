using System.ComponentModel.DataAnnotations;
using EcommerceAPI.Models;

namespace EcommerceAPI.DTOs
{
    public class CreateReturnDto
    {
        [Range(1, int.MaxValue)]
        public int OrderItemId { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; }

        [Required, MaxLength(60)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;
    }

    public class UpdateReturnStatusDto
    {
        public ReturnStatus Status { get; set; }

        [MaxLength(1000)]
        public string AdminNote { get; set; } = string.Empty;
    }

    public class ReturnRequestDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public ReturnStatus Status { get; set; }
        public string AdminNote { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
