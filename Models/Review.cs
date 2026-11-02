namespace EcommerceAPI.Models
{
    public class Review
    {
        public int Id { get; set; }

        // Foreign keys
        public int ProductId { get; set; }
        public int UserId { get; set; }

        /// <summary>Note de 1 à 5 étoiles.</summary>
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relations
        public Product Product { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}