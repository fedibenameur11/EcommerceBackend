using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceAPI.Services;
using EcommerceAPI.DTOs;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _svc;
        public CategoriesController(ICategoryService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
        {
            var list = await _svc.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryDto>> Get(int id)
        {
            var item = await _svc.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryDto dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryDto dto)
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
