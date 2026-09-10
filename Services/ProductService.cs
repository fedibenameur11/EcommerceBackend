using EcommerceAPI.DTOs;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;

namespace EcommerceAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        public ProductService(IProductRepository repo) => _repo = repo;

        public async Task<ProductPageDto> GetAllAsync(int page, int pageSize, string? search, int? categoryId)
        {
            var result = await _repo.GetPagedAsync(page, pageSize, search, categoryId);
            return new ProductPageDto
            {
                Items = result.Items.Select(ToDto), Page = page,
                PageSize = pageSize, TotalCount = result.TotalCount
            };
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var p = await _repo.GetByIdAsync(id);
            if (p == null) return null;
            return ToDto(p);
        }

        private static ProductDto ToDto(Product p) => new()
        {
            Id = p.Id, Name = p.Name, Description = p.Description, Price = p.Price,
            StockQuantity = p.StockQuantity, CategoryId = p.CategoryId, ImageUrl = p.ImageUrl
        };

        public async Task<ProductDto> CreateAsync(ProductDto dto)
        {
            var entity = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                ImageUrl = dto.ImageUrl
            };
            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();
            dto.Id = entity.Id;
            return dto;
        }

        public async Task<bool> UpdateAsync(int id, ProductDto dto)
        {
            var p = await _repo.GetByIdAsync(id);
            if (p == null) return false;
            p.Name = dto.Name;
            p.Description = dto.Description;
            p.Price = dto.Price;
            p.StockQuantity = dto.StockQuantity;
            p.CategoryId = dto.CategoryId;
            p.ImageUrl = dto.ImageUrl;
            _repo.Update(p);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var p = await _repo.GetByIdAsync(id);
            if (p == null) return false;
            _repo.Remove(p);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
