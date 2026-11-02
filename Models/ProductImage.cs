namespace EcommerceAPI.Models
{
    /// <summary>
    /// Image supplémentaire d'un produit (galerie de la page détail).
    /// L'image de couverture de la liste reste Product.ImageUrl.
    /// </summary>
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Url { get; set; } = string.Empty;
        public int SortOrder { get; set; }

        public Product Product { get; set; } = null!;
    }
}
