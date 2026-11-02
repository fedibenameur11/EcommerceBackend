namespace EcommerceAPI.Models
{
    public class Product
    { 
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        /// <summary>Marque du produit (vide = sans marque).</summary>
        public string Brand { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        /// <summary>Public visé : "Homme", "Femme" ou vide (mixte / non précisé).</summary>
        public string Gender { get; set; } = string.Empty;
        /// <summary>Couleur principale du produit (voir ProductOptions.Colors) ; vide = non précisée.</summary>
        public string Color { get; set; } = string.Empty;
        /// <summary>Pointures ou tailles disponibles, séparées par des virgules (ex. "38,39,40" ou "S,M,L"). Vide = pas de taille.</summary>
        public string Sizes { get; set; } = string.Empty;

        public List<string> SizeList()
            => Sizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        // Vente flash programmée : SalePrice s'applique entre SaleStartsAt et SaleEndsAt
        public decimal? SalePrice { get; set; }
        public DateTime? SaleStartsAt { get; set; }
        public DateTime? SaleEndsAt { get; set; }

        public bool IsOnSale(DateTime nowUtc)
            => SalePrice.HasValue && SaleEndsAt.HasValue
               && (!SaleStartsAt.HasValue || SaleStartsAt.Value <= nowUtc) && SaleEndsAt.Value > nowUtc
               && SalePrice.Value < Price;

        /// <summary>Prix réellement facturé : prix de vente flash s'il est en cours, sinon prix normal.</summary>
        public decimal CurrentPrice(DateTime nowUtc) => IsOnSale(nowUtc) ? SalePrice!.Value : Price;

        // Foreign Keys
        public int CategoryId { get; set; }
        /// <summary>Vendeur propriétaire du produit (null = produit de la boutique, géré par l'admin).</summary>
        public int? SellerId { get; set; }

        // Relations
        public Category Category { get; set; } = null!;
        public User? Seller { get; set; }
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
