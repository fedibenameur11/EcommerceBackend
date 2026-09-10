using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<UserDto?> RegisterAsync(RegisterDto dto);
    }
}
