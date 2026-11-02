using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/admin/analytics")]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _svc;
        public AnalyticsController(IAnalyticsService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<AnalyticsDto>> Get(
            [FromQuery] int days = 30,
            [FromQuery] int top = 5,
            [FromQuery] int lowStockThreshold = 5)
        {
            if (days is < 1 or > 365) return BadRequest("days doit être compris entre 1 et 365.");
            if (top is < 1 or > 50) return BadRequest("top doit être compris entre 1 et 50.");
            if (lowStockThreshold < 0) return BadRequest("lowStockThreshold doit être positif.");
            return Ok(await _svc.GetAsync(days, top, lowStockThreshold, null));
        }
    }
}