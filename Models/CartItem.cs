namespace EcommerceAPI.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        // Foreign keys
        public int CartId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Relations
        public Cart Cart { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}