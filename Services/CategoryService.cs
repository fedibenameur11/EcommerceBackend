using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;

namespace EcommerceAPI.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        public CategoryService(ICategoryRepository repo) => _repo = repo;

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var items = await _repo.GetAllAsync();
            var counts = await _repo.GetProductCountsAsync();
            return items.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                ProductCount = counts.GetValueOrDefault(c.Id)
            }).ToList();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c == null) return null;
            return new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl
            };
        }

        public async Task<CategoryDto> CreateAsync(CategoryDto dto)
        {
            var entity = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl
            };
            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();
            dto.Id = entity.Id;
            return dto;
        }

        public async Task<bool> UpdateAsync(int id, CategoryDto dto)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c == null) return false;
            c.Name = dto.Name;
            c.Description = dto.Description;
            c.ImageUrl = dto.ImageUrl;
            _repo.Update(c);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c == null) return false;
            _repo.Remove(c);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
