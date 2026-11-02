using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceAPI.Services;
using EcommerceAPI.Repositories;
using EcommerceAPI.DTOs;
using System.Security.Claims;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _svc;
        public ProductsController(IProductService svc) => _svc = svc;

        /// <summary>
        /// Liste paginée. Filtres : search, categoryId, minPrice, maxPrice, brand, gender, color, size, minRating.
        /// Tri : default | price-asc | price-desc | name | newest.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ProductPageDto>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? brand = null,
            [FromQuery] string? gender = null,
            [FromQuery] string? color = null,
            [FromQuery] string? size = null,
            [FromQuery] bool? onSale = null,
            [FromQuery] int? minRating = null,
            [FromQuery] string? sort = null)
        {
            var list = await _svc.GetAllAsync(new ProductQuery
            {
                Page = page, PageSize = pageSize, Search = search, CategoryId = categoryId,
                MinPrice = minPrice, MaxPrice = maxPrice, Brand = brand, Gender = gender, Color = color, Size = size, OnSale = onSale, MinRating = minRating, Sort = sort
            });
            return Ok(list);
        }

        /// <summary>Marques disponibles (pour le filtre).</summary>
        [HttpGet("brands")]
        public async Task<ActionResult<IEnumerable<string>>> Brands() => Ok(await _svc.GetBrandsAsync());

        /// <summary>Suggestions affichées pendant la saisie dans la barre de recherche.</summary>
        [HttpGet("suggestions")]
        public async Task<ActionResult<IEnumerable<ProductSuggestionDto>>> Suggestions(
            [FromQuery] string q = "", [FromQuery] int take = 6)
            => Ok(await _svc.GetSuggestionsAsync(q, take));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDto>> Get(int id)
        {
            var item = await _svc.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>Produits du vendeur connecté.</summary>
        [HttpGet("mine")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<ProductPageDto>> Mine(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _svc.GetAllAsync(new ProductQuery
            {
                Page = page, PageSize = pageSize, SellerId = CurrentUserId()
            }));

        [HttpPost]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<ActionResult<ProductDto>> Create([FromBody] ProductDto dto)
        {
            var created = await _svc.CreateAsync(dto, SellerScope());
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
        {
            if (!await _svc.UpdateAsync(id, dto, SellerScope())) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _svc.DeleteAsync(id, SellerScope())) return NotFound();
            return NoContent();
        }

        private int CurrentUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Id du vendeur si l'utilisateur est un vendeur (restreint à ses produits), null pour un admin.</summary>
        private int? SellerScope() => User.IsInRole("Seller") ? CurrentUserId() : null;
    }
}
