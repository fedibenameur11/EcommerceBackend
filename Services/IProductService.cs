using EcommerceAPI.DTOs;
using EcommerceAPI.Repositories;

namespace EcommerceAPI.Services
{
    public interface IProductService
    {
        Task<ProductPageDto> GetAllAsync(ProductQuery query);
        Task<IEnumerable<string>> GetBrandsAsync();
        Task<IEnumerable<ProductSuggestionDto>> GetSuggestionsAsync(string term, int take = 6);
        Task<ProductDto?> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(ProductDto dto, int? sellerId = null);
        /// <summary>sellerId non null = vendeur : il ne peut modifier que ses propres produits.</summary>
        Task<bool> UpdateAsync(int id, ProductDto dto, int? sellerId = null);
        Task<bool> DeleteAsync(int id, int? sellerId = null);
    }
}
