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
            if (page < 1) throw new ArgumentException("page doit être supérieur à 0.");
            if (pageSize is < 1 or > 100) throw new ArgumentException("pageSize doit être entre 1 et 100.");
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
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Le nom du produit est obligatoire.");
            if (dto.Price < 0) throw new ArgumentException("Le prix doit être supérieur ou égal à zéro.");
            if (dto.StockQuantity < 0) throw new ArgumentException("Le stock doit être supérieur ou égal à zéro.");
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
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Le nom du produit est obligatoire.");
            if (dto.Price < 0) throw new ArgumentException("Le prix doit être supérieur ou égal à zéro.");
            if (dto.StockQuantity < 0) throw new ArgumentException("Le stock doit être supérieur ou égal à zéro.");
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
            p.IsActive = false;
            _repo.Update(p);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
