namespace EcommerceAPI.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        /// <summary>Renseigné par le serveur : nombre de produits actifs de la catégorie.</summary>
        public int ProductCount { get; set; }
    }
}
