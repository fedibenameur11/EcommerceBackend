using System.Security.Claims;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/returns")]
    public class ReturnsController : ControllerBase
    {
        private readonly IReturnService _svc;
        public ReturnsController(IReturnService svc) => _svc = svc;

        /// <summary>Motifs proposés dans le formulaire et délai de retour.</summary>
        [HttpGet("options")]
        public ActionResult<object> Options() => Ok(new { reasons = ReturnService.Reasons, windowDays = ReturnService.ReturnWindowDays });

        [HttpPost]
        [Authorize(Roles = "Customer,Seller")]
        public async Task<ActionResult<ReturnRequestDto>> Create([FromBody] CreateReturnDto dto)
            => Ok(await _svc.CreateAsync(CurrentUserId(), dto));

        [HttpGet("mine")]
        [Authorize(Roles = "Customer,Seller")]
        public async Task<ActionResult<IEnumerable<ReturnRequestDto>>> Mine()
            => Ok(await _svc.GetMineAsync(CurrentUserId()));

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ReturnRequestDto>>> GetAll([FromQuery] ReturnStatus? status = null)
            => Ok(await _svc.GetAllAsync(status));

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReturnStatusDto dto)
            => await _svc.UpdateStatusAsync(id, dto) ? NoContent() : NotFound();

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
