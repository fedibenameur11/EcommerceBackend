using Microsoft.AspNetCore.Mvc;
using EcommerceAPI.DTOs;
using EcommerceAPI.Services;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto dto)
        {
            var user = await _authService.RegisterAsync(dto);
            if (user == null) return Conflict("Cette adresse e-mail existe déjà.");
            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto login)
        {
            if (string.IsNullOrWhiteSpace(login?.Email) || string.IsNullOrWhiteSpace(login.Password))
                return BadRequest("L'e-mail et le mot de passe sont obligatoires.");

            var response = await _authService.LoginAsync(login);
            return response == null ? Unauthorized() : Ok(response);
        }
    }
}
