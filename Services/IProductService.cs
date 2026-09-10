using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface IProductService
    {
        Task<ProductPageDto> GetAllAsync(int page, int pageSize, string? search, int? categoryId);
        Task<ProductDto?> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(ProductDto dto);
        Task<bool> UpdateAsync(int id, ProductDto dto);
        Task<bool> DeleteAsync(int id);
    }
}