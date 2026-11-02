namespace EcommerceAPI.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }

        // Foreign keys
        public int UserId { get; set; }
        public int ProductId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Relations
        public User User { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}