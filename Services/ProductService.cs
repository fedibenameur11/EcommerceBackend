using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;

namespace EcommerceAPI.Services
{
    public class ProductService : IProductService
    {
        private static readonly HashSet<string> SortKeys = new() { "default", "price-asc", "price-desc", "name", "newest" };

        private readonly IProductRepository _repo;
        public ProductService(IProductRepository repo) => _repo = repo;

        public async Task<ProductPageDto> GetAllAsync(ProductQuery query)
        {
            if (query.Page < 1) throw new ArgumentException("page doit être supérieur à 0.");
            if (query.PageSize is < 1 or > 100) throw new ArgumentException("pageSize doit être compris entre 1 et 100.");
            if (query.MinPrice < 0 || query.MaxPrice < 0) throw new ArgumentException("Les prix doivent être positifs.");
            if (query.MinPrice > query.MaxPrice) throw new ArgumentException("Le prix minimum dépasse le prix maximum.");
            if (query.MinRating is < 1 or > 5) throw new ArgumentException("La note minimale doit être comprise entre 1 et 5.");
            if (!string.IsNullOrEmpty(query.Sort) && !SortKeys.Contains(query.Sort))
                throw new ArgumentException("Tri inconnu. Valeurs possibles : " + string.Join(", ", SortKeys) + ".");

            var result = await _repo.GetPagedAsync(query);
            return new ProductPageDto
            {
                Items = await ToDtosAsync(result.Items), Page = query.Page,
                PageSize = query.PageSize, TotalCount = result.TotalCount
            };
        }

        public async Task<IEnumerable<string>> GetBrandsAsync() => await _repo.GetBrandsAsync();

        public async Task<IEnumerable<ProductSuggestionDto>> GetSuggestionsAsync(string term, int take = 6)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2) return Enumerable.Empty<ProductSuggestionDto>();
            var products = await _repo.GetSuggestionsAsync(term, Math.Clamp(take, 1, 10));
            return products.Select(p => new ProductSuggestionDto
            {
                Id = p.Id, Name = p.Name, Price = p.Price, ImageUrl = p.ImageUrl,
                CategoryName = p.Category?.Name ?? string.Empty
            });
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var p = await _repo.GetDetailedByIdAsync(id);
            if (p == null) return null;
            return (await ToDtosAsync(new[] { p })).First();
        }

        private async Task<List<ProductDto>> ToDtosAsync(IEnumerable<Product> products)
        {
            var list = products.ToList();
            var ratings = await _repo.GetRatingSummariesAsync(list.Select(p => p.Id));
            return list.Select(p =>
            {
                var dto = ToDto(p);
                if (ratings.TryGetValue(p.Id, out var r)) { dto.AverageRating = r.Average; dto.ReviewCount = r.Count; }
                return dto;
            }).ToList();
        }

        private static ProductDto ToDto(Product p) => new()
        {
            Id = p.Id, Name = p.Name, Description = p.Description, Price = p.CurrentPrice(DateTime.UtcNow),
            StockQuantity = p.StockQuantity, CategoryId = p.CategoryId, ImageUrl = p.ImageUrl,
            Brand = p.Brand, SellerId = p.SellerId, Gender = p.Gender, Color = p.Color, Sizes = p.SizeList(),
            SalePrice = p.SalePrice, SaleStartsAt = p.SaleStartsAt, SaleEndsAt = p.SaleEndsAt,
            OnSale = p.IsOnSale(DateTime.UtcNow),
            OriginalPrice = p.IsOnSale(DateTime.UtcNow) ? p.Price : null,
            Images = p.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => i.Url).ToList()
        };

        private static void Validate(ProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Le nom du produit est obligatoire.");
            if (dto.Price < 0) throw new ArgumentException("Le prix doit être supérieur ou égal à zéro.");
            if (dto.StockQuantity < 0) throw new ArgumentException("Le stock doit être supérieur ou égal à zéro.");
            if (dto.SalePrice.HasValue)
            {
                if (dto.SalePrice.Value <= 0 || dto.SalePrice.Value >= dto.Price)
                    throw new ArgumentException("Le prix de vente flash doit être positif et inférieur au prix normal.");
                if (!dto.SaleEndsAt.HasValue)
                    throw new ArgumentException("La date de fin de la vente flash est obligatoire.");
                if (dto.SaleStartsAt.HasValue && dto.SaleStartsAt >= dto.SaleEndsAt)
                    throw new ArgumentException("La vente flash doit se terminer après son début.");
            }
            if (!string.IsNullOrWhiteSpace(dto.Gender) && !ProductOptions.Genders.Contains(dto.Gender.Trim()))
                throw new ArgumentException("Le genre doit être « Homme » ou « Femme ».");
            if (!string.IsNullOrWhiteSpace(dto.Color) && !ProductOptions.Colors.Contains(dto.Color.Trim()))
                throw new ArgumentException($"Couleur inconnue : « {dto.Color} ». Valeurs possibles : {string.Join(", ", ProductOptions.Colors)}.");
            var badSize = dto.Sizes.FirstOrDefault(s => !ProductOptions.AllSizes.Contains(s?.Trim() ?? string.Empty));
            if (badSize != null)
                throw new ArgumentException($"Taille inconnue : « {badSize} ». Valeurs possibles : {string.Join(", ", ProductOptions.AllSizes)}.");
            var cleaned = CleanSizes(dto);
            if (cleaned.Intersect(ProductOptions.ShoeSizes).Any() && cleaned.Intersect(ProductOptions.ClothingSizes).Any())
                throw new ArgumentException("Choisissez soit des pointures (38-45), soit des tailles (XS-XXL), pas les deux.");
            if (dto.Images.Count > ProductDto.MaxGalleryImages)
                throw new ArgumentException($"Un produit peut avoir au plus {ProductDto.MaxGalleryImages} images supplémentaires.");
        }

        /// <summary>Tailles dédoublonnées et triées dans l'ordre naturel (38 → 45, XS → XXL).</summary>
        private static List<string> CleanSizes(ProductDto dto)
            => dto.Sizes
                .Select(s => s?.Trim() ?? string.Empty)
                .Where(s => s.Length > 0)
                .Distinct()
                .OrderBy(s => Array.IndexOf(ProductOptions.AllSizes, s))
                .ToList();

        private static string GenderOf(ProductDto dto) => (dto.Gender ?? string.Empty).Trim();

        private static string ColorOf(ProductDto dto) => (dto.Color ?? string.Empty).Trim();

        /// <summary>Nettoie la galerie : pas de valeur vide, pas de doublon, pas la couverture elle-même.</summary>
        private static List<string> CleanGallery(ProductDto dto)
            => dto.Images
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Where(u => u != dto.ImageUrl)
                .Distinct()
                .ToList();

        public async Task<ProductDto> CreateAsync(ProductDto dto, int? sellerId = null)
        {
            Validate(dto);
            var entity = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                ImageUrl = dto.ImageUrl,
                Brand = (dto.Brand ?? string.Empty).Trim(),
                Gender = GenderOf(dto),
                Color = ColorOf(dto),
                Sizes = string.Join(',', CleanSizes(dto)),
                SalePrice = dto.SalePrice, SaleStartsAt = dto.SaleStartsAt?.ToUniversalTime(), SaleEndsAt = dto.SaleEndsAt?.ToUniversalTime(),
                SellerId = sellerId
            };
            var gallery = CleanGallery(dto);
            for (var i = 0; i < gallery.Count; i++)
                entity.Images.Add(new ProductImage { Url = gallery[i], SortOrder = i });

            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();
            dto.Id = entity.Id;
            dto.SellerId = entity.SellerId;
            dto.Images = gallery;
            dto.Gender = entity.Gender;
            dto.Color = entity.Color;
            dto.Sizes = entity.SizeList();
            return dto;
        }

        public async Task<bool> UpdateAsync(int id, ProductDto dto, int? sellerId = null)
        {
            Validate(dto);
            var p = await _repo.GetWithImagesAsync(id);
            if (p == null || (sellerId.HasValue && p.SellerId != sellerId)) return false;
            p.Name = dto.Name;
            p.Description = dto.Description;
            p.Price = dto.Price;
            p.StockQuantity = dto.StockQuantity;
            p.CategoryId = dto.CategoryId;
            p.ImageUrl = dto.ImageUrl;
            p.Brand = (dto.Brand ?? string.Empty).Trim();
            p.Gender = GenderOf(dto);
            p.Color = ColorOf(dto);
            p.Sizes = string.Join(',', CleanSizes(dto));
            p.SalePrice = dto.SalePrice;
            p.SaleStartsAt = dto.SaleStartsAt?.ToUniversalTime();
            p.SaleEndsAt = dto.SaleEndsAt?.ToUniversalTime();

            // La galerie est remplacée en bloc : on garde les lignes inchangées et on ajuste le reste
            var gallery = CleanGallery(dto);
            foreach (var removed in p.Images.Where(i => !gallery.Contains(i.Url)).ToList())
                p.Images.Remove(removed);
            for (var i = 0; i < gallery.Count; i++)
            {
                var existing = p.Images.FirstOrDefault(x => x.Url == gallery[i]);
                if (existing != null) existing.SortOrder = i;
                else p.Images.Add(new ProductImage { Url = gallery[i], SortOrder = i });
            }

            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int? sellerId = null)
        {
            var p = await _repo.GetByIdAsync(id);
            if (p == null || (sellerId.HasValue && p.SellerId != sellerId)) return false;
            p.IsActive = false;
            _repo.Update(p);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
