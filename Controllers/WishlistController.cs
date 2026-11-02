using System.Security.Claims;
using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer,Seller")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _svc;
        public WishlistController(IWishlistService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> Get()
            => Ok(await _svc.GetAsync(CurrentUserId()));

        [HttpPost("{productId:int}")]
        public async Task<IActionResult> Add(int productId)
            => await _svc.AddAsync(CurrentUserId(), productId) ? NoContent() : NotFound();

        [HttpDelete("{productId:int}")]
        public async Task<IActionResult> Remove(int productId)
        {
            await _svc.RemoveAsync(CurrentUserId(), productId);
            return NoContent();
        }

        private int CurrentUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}