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
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _svc;
        public OrdersController(IOrderService svc) => _svc = svc;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderDto>> Get(int id)
        {
            var order = await _svc.GetByIdAsync(id);
            if (order == null) return NotFound();
            if (!User.IsInRole("Admin") && order.UserId != CurrentUserId()) return Forbid();
            return Ok(order);
        }

        [HttpGet("mine")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> Mine()
        {
            var userId = CurrentUserId();
            return Ok(await _svc.GetByUserIdAsync(userId));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
            => Ok(await _svc.GetAllAsync());

        [HttpPost]
        public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
        {
            var created = await _svc.CreateAsync(dto, CurrentUserId());
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
            => await _svc.UpdateStatusAsync(id, dto) ? NoContent() : NotFound();

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
            => await _svc.CancelAsync(id, CurrentUserId(), User.IsInRole("Admin")) ? NoContent() : BadRequest("Commande introuvable ou non annulable.");

        private int CurrentUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
