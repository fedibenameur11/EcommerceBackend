using System.Collections.Generic;
using EcommerceAPI.Models;

namespace EcommerceAPI.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        /// <summary>Livreur affecté par l'admin (null = pas encore affectée).</summary>
        public int? DeliveryPersonId { get; set; }
        public string? DeliveryPersonName { get; set; }
        public string? DeliveryPersonPhone { get; set; }
        public DeliveryAssignmentStatus DeliveryAssignment { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Currency { get; set; } = "TND";
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        /// <summary>Code promo utilisé (null = aucun) et remise appliquée en TND.</summary>
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
    }
}
