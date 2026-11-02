using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _svc;
        public CouponsController(ICouponService svc) => _svc = svc;

        /// <summary>Aperçu de la remise d'un code pour un sous-total donné (panier / paiement).</summary>
        [HttpPost("validate")]
        [Authorize]
        public async Task<ActionResult<CouponValidationDto>> Validate([FromBody] ValidateCouponDto dto)
            => Ok(await _svc.ValidateAsync(dto.Code, dto.Subtotal, dto.Items));

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<CouponDto>>> GetAll()
            => Ok(await _svc.GetAllAsync());

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CouponDto>> Create([FromBody] SaveCouponDto dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveCouponDto dto)
            => await _svc.UpdateAsync(id, dto) ? NoContent() : NotFound();

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
            => await _svc.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
