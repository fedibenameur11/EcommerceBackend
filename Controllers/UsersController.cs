using System.Security.Claims;
using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;

        public UsersController(IUserService service) => _service = service;

        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetMe()
        {
            var user = await _service.GetByIdAsync(CurrentUserId());
            return user == null ? NotFound() : Ok(user);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(UpdateUserDto dto)
            => await _service.UpdateAsync(CurrentUserId(), dto) ? NoContent() : NotFound();

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserPageDto>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
            => Ok(await _service.GetAllAsync(page, pageSize));

        /// <summary>Admin : livreurs actifs (liste déroulante d'affectation).</summary>
        [HttpGet("delivery")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> DeliveryPeople()
            => Ok(await _service.GetDeliveryPeopleAsync());

        /// <summary>Admin : crée un compte livreur.</summary>
        [HttpPost("delivery")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> CreateDeliveryPerson(CreateDeliveryPersonDto dto)
        {
            var created = await _service.CreateDeliveryPersonAsync(dto);
            return created == null ? Conflict("Cette adresse e-mail existe déjà.") : Ok(created);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var user = await _service.GetByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, UpdateUserDto dto)
            => await _service.UpdateAsync(id, dto) ? NoContent() : NotFound();

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();

        private int CurrentUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
