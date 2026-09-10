using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace EcommerceAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository users, IConfiguration configuration)
        {
            _users = users;
            _configuration = configuration;
        }

        public async Task<UserDto?> RegisterAsync(RegisterDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            if (await _users.GetByEmailAsync(email) != null) return null;

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = email,
                Password = dto.Password,
                PhoneNumber = dto.PhoneNumber.Trim(),
                Address = dto.Address.Trim(),
                Role = UserRole.Customer
            };

            await _users.AddAsync(user);
            await _users.SaveChangesAsync();
            return ToDto(user);
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _users.GetByEmailAsync(email);
            if (user == null || user.Password != dto.Password) return null;

            var jwt = _configuration.GetSection("Jwt");
            var key = jwt["Key"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("La clé JWT n'est pas configurée.");

            var minutes = int.TryParse(jwt["ExpiresInMinutes"], out var value) ? value : 60;
            var expiresAt = DateTime.UtcNow.AddMinutes(minutes);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"], audience: jwt["Audience"], claims: claims,
                expires: expiresAt, signingCredentials: credentials);

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt
            };
        }

        private static UserDto ToDto(User user) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role
        };
    }
}
