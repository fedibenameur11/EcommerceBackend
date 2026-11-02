using EcommerceAPI.Models;

namespace EcommerceAPI.Repositories
{
    /// <summary>Critères de recherche, de filtre, de tri et de pagination des produits.</summary>
    public class ProductQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? SellerId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Brand { get; set; }
        /// <summary>"Homme" ou "Femme" (les produits mixtes, sans genre, apparaissent dans les deux).</summary>
        public string? Gender { get; set; }
        /// <summary>Couleur exacte (ex. "Noir").</summary>
        public string? Color { get; set; }
        /// <summary>Taille ou pointure proposée par le produit (ex. "M", "42").</summary>
        public string? Size { get; set; }
        /// <summary>Note moyenne minimale (1 à 5).</summary>
        public int? MinRating { get; set; }
        /// <summary>true = uniquement les produits en vente flash en ce moment.</summary>
        public bool? OnSale { get; set; }
        /// <summary>default | price-asc | price-desc | name | newest</summary>
        public string? Sort { get; set; }
    }

    public interface IProductRepository
    {
        Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(ProductQuery query);
        Task<List<Product>> GetSuggestionsAsync(string term, int take);
        Task<List<string>> GetBrandsAsync();
        Task<Product?> GetByIdAsync(int id);
        /// <summary>Lecture seule : produit avec sa galerie d'images.</summary>
        Task<Product?> GetDetailedByIdAsync(int id);
        /// <summary>Suivi activé : produit avec sa galerie, pour la modification.</summary>
        Task<Product?> GetWithImagesAsync(int id);
        Task<Dictionary<int, (double Average, int Count)>> GetRatingSummariesAsync(IEnumerable<int> productIds);
        Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids);
        Task AddAsync(Product product);
        void Update(Product product);
        void Remove(Product product);
        Task<int> SaveChangesAsync();
    }
}
