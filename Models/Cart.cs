using System.Collections.Generic;

namespace EcommerceAPI.Models
{
    public class Cart
    {
        public int Id { get; set; }

        // Foreign Key
        public int UserId { get; set; }

        // Relations
        public User User { get; set; } = null!;
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}