using System;

namespace EcommerceAPI.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        // Foreign keys
        public int OrderId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // Relations
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}