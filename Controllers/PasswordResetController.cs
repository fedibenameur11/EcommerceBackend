using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IPasswordResetService _passwordReset;

        public PasswordResetController(IPasswordResetService passwordReset)
        {
            _passwordReset = passwordReset;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _passwordReset.RequestCodeAsync(dto.Email);
            return Ok(new { message = "Si un compte existe pour cette adresse, un code de vérification vient d'être envoyé." });
        }

        [HttpPost("verify-reset-code")]
        public async Task<ActionResult<VerifyResetCodeResponseDto>> VerifyResetCode([FromBody] VerifyResetCodeDto dto)
        {
            var token = await _passwordReset.VerifyCodeAsync(dto.Email, dto.Code);
            return Ok(new VerifyResetCodeResponseDto { ResetToken = token });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _passwordReset.ResetPasswordAsync(dto.Email, dto.ResetToken, dto.NewPassword);
            return Ok(new { message = "Votre mot de passe a été réinitialisé. Vous pouvez maintenant vous connecter." });
        }
    }
}
