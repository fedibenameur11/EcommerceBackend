using System.Security.Claims;
using EcommerceAPI.Repositories;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    /// <summary>Documents PDF d'une commande : facture (client / admin).</summary>
    [ApiController]
    [Route("api/orders/{id:int}")]
    [Authorize]
    public class OrderDocumentsController : ControllerBase
    {
        private readonly IOrderRepository _orders;
        private readonly OrderDocumentService _documents;

        public OrderDocumentsController(IOrderRepository orders, OrderDocumentService documents)
        {
            _orders = orders;
            _documents = documents;
        }

        [HttpGet("invoice")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _orders.GetDetailedByIdAsync(id);
            if (order == null) return NotFound();
            if (!User.IsInRole("Admin") && order.UserId != CurrentUserId()) return Forbid();
            if (order.Status == Models.OrderStatus.Cancelled)
                return BadRequest("Aucune facture pour une commande annulée.");
            return File(_documents.BuildInvoice(order), "application/pdf", $"facture-{id}.pdf");
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
