using System.Security.Claims;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    /// <summary>Espace livreur : commandes qui lui sont affectées par l'admin.</summary>
    [ApiController]
    [Route("api/delivery")]
    [Authorize(Roles = "Delivery")]
    public class DeliveryController : ControllerBase
    {
        private readonly IOrderService _orders;
        public DeliveryController(IOrderService orders) => _orders = orders;

        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> Orders()
            => Ok(await _orders.GetByDeliveryPersonAsync(CurrentUserId()));

        /// <summary>Accepter ou refuser une affectation en attente.</summary>
        [HttpPut("orders/{id:int}/response")]
        public async Task<IActionResult> Respond(int id, DeliveryResponseDto dto)
            => await _orders.RespondToAssignmentAsync(id, CurrentUserId(), dto.Accept) ? NoContent() : NotFound();

        /// <summary>Livraisons effectuées et gains sur les N derniers jours (7 = semaine, 30 = mois).</summary>
        [HttpGet("stats")]
        public async Task<ActionResult<DeliveryStatsDto>> Stats([FromQuery] int days = 7)
        {
            if (days is < 1 or > 365) return BadRequest("days doit être compris entre 1 et 365.");
            return Ok(await _orders.GetDeliveryStatsAsync(CurrentUserId(), days));
        }

        /// <summary>Processing -> Shipped (en route) puis Shipped -> Delivered.</summary>
        [HttpPut("orders/{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, DeliveryStatusDto dto)
            => await _orders.UpdateDeliveryStatusAsync(id, CurrentUserId(), dto.Status) ? NoContent() : NotFound();

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
