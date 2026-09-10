using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceAPI.Services;
using EcommerceAPI.DTOs;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _svc;
        public ProductsController(IProductService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<ProductPageDto>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null)
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return BadRequest("page doit être supérieur à 0 et pageSize doit être compris entre 1 et 100.");
            var list = await _svc.GetAllAsync(page, pageSize, search, categoryId);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDto>> Get(int id)
        {
            var item = await _svc.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> Create([FromBody] ProductDto dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
        {
            if (!await _svc.UpdateAsync(id, dto)) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _svc.DeleteAsync(id)) return NotFound();
            return NoContent();
        }
    }
}