using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceAPI.Services;
using EcommerceAPI.DTOs;
using System.Security.Claims;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer,Admin")]
    public class CartsController : ControllerBase
    {
        private readonly ICartService _svc;
        public CartsController(ICartService svc) => _svc = svc;

        [HttpGet("{userId:int}")]
        public async Task<ActionResult<CartDto>> GetCart(int userId)
        {
            if (!CanAccessUser(userId)) return Forbid();
            var cart = await _svc.GetCartAsync(userId);
            return Ok(cart);
        }

        [HttpPost("{userId:int}/items")]
        public async Task<ActionResult<CartDto>> AddItem(int userId, [FromBody] CartItemDto item)
        {
            if (!CanAccessUser(userId)) return Forbid();
            var cart = await _svc.AddItemAsync(userId, item);
            return Ok(cart);
        }

        [HttpPut("{userId:int}/items/{productId:int}")]
        public async Task<ActionResult<CartDto>> UpdateQuantity(int userId, int productId, [FromBody] int quantity)
        {
            if (!CanAccessUser(userId)) return Forbid();
            var cart = await _svc.UpdateItemQuantityAsync(userId, productId, quantity);
            if (cart == null) return NotFound();
            return Ok(cart);
        }

        [HttpDelete("{userId:int}/items/{productId:int}")]
        public async Task<IActionResult> RemoveItem(int userId, int productId)
        {
            if (!CanAccessUser(userId)) return Forbid();
            if (!await _svc.RemoveItemAsync(userId, productId)) return NotFound();
            return NoContent();
        }

        private bool CanAccessUser(int userId)
            => User.IsInRole("Admin") || User.FindFirstValue(ClaimTypes.NameIdentifier) == userId.ToString();
    }
}
