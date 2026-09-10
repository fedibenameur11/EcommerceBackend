using System.Collections.Generic;
using EcommerceAPI.Models;

namespace EcommerceAPI.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
