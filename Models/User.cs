namespace EcommerceAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Customer;
        public bool IsActive { get; set; } = true;

        // Relations
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        /// <summary>Commandes affectées à ce livreur (rôle Delivery).</summary>
        public ICollection<Order> DeliveryOrders { get; set; } = new List<Order>();
        public Cart Cart { get; set; } = null!;
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    }
    public enum UserRole
    {
        Customer = 0,
        Admin = 1,
        Seller = 2,
        /// <summary>Livreur : reçoit les commandes affectées par l'admin et les livre.</summary>
        Delivery = 3
    }
}