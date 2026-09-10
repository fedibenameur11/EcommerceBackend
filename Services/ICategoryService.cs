using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<CategoryDto> CreateAsync(CategoryDto dto);
        Task<bool> UpdateAsync(int id, CategoryDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
