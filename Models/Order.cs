using System;
using System.Collections.Generic;

namespace EcommerceAPI.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Foreign Key
        public int UserId { get; set; }

        public string OrderNumber { get; set; } = string.Empty; // ex: "#1025"
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime ShippedDate { get; set; } = DateTime.MinValue;
        public DateTime DeliveredDate { get; set; } = DateTime.MinValue;

        // Relations
        public User User { get; set; } = null!;
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }

}