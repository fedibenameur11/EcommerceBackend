using System.Collections.Generic;

namespace EcommerceAPI.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        // Relations
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}