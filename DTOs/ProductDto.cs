using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class ProductDto
    {
        public const int MaxGalleryImages = 8;

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// En lecture : prix actuel (prix de vente flash s'il est en cours).
        /// En écriture (création / modification) : prix normal du produit.
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>Prix normal barré, renseigné seulement pendant une vente flash.</summary>
        public decimal? OriginalPrice { get; set; }
        /// <summary>Vente flash programmée (prix, début, fin) ; ignorée si vide.</summary>
        public decimal? SalePrice { get; set; }
        public DateTime? SaleStartsAt { get; set; }
        public DateTime? SaleEndsAt { get; set; }
        public bool OnSale { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string Brand { get; set; } = string.Empty;
        /// <summary>"Homme", "Femme" ou vide (mixte).</summary>
        public string Gender { get; set; } = string.Empty;
        /// <summary>Couleur principale (Noir, Blanc, Rouge...) ou vide.</summary>
        public string Color { get; set; } = string.Empty;
        /// <summary>Pointures (38 à 45) ou tailles (XS à XXL) disponibles ; vide = produit sans taille.</summary>
        public List<string> Sizes { get; set; } = new();
        /// <summary>Image de couverture : affichée dans la liste des produits.</summary>
        public string ImageUrl { get; set; } = string.Empty;
        /// <summary>
        /// Images supplémentaires, affichées dans la galerie de la page détail.
        /// Renseignées uniquement par GET /products/{id}, vides dans les listes.
        /// </summary>
        [MaxLength(MaxGalleryImages)]
        public List<string> Images { get; set; } = new();
        /// <summary>Renseigné par le serveur : vendeur propriétaire (null = boutique).</summary>
        public int? SellerId { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    /// <summary>Résultat léger de la recherche en cours de frappe.</summary>
    public class ProductSuggestionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}
