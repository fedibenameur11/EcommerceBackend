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

        // Relations
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public Cart Cart { get; set; } = null!;
    }
    public enum UserRole
    {
        Customer = 0,
        Admin = 1
    }
}