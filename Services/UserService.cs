using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;
using Microsoft.AspNetCore.Identity;

namespace EcommerceAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository) => _repository = repository;

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            return user == null ? null : ToDto(user);
        }

        public async Task<UserPageDto> GetAllAsync(int page = 1, int pageSize = 10)
        {
            if (page < 1 || pageSize is < 1 or > 100) throw new ArgumentException("Pagination invalide.");
            var result = await _repository.GetPagedAsync(page, pageSize);
            return new UserPageDto
            {
                Items = result.Items.Select(ToDto),
                Page = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null) return false;
            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = dto.PhoneNumber.Trim();
            user.Address = dto.Address.Trim();
            _repository.Update(user);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserDto>> GetDeliveryPeopleAsync()
            => (await _repository.GetActiveByRoleAsync(UserRole.Delivery)).Select(ToDto);

        public async Task<UserDto?> CreateDeliveryPersonAsync(CreateDeliveryPersonDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            if (await _repository.GetByEmailAsync(email) != null) return null;

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = email,
                PhoneNumber = dto.PhoneNumber.Trim(),
                Address = string.Empty,
                Role = UserRole.Delivery
            };
            user.Password = new PasswordHasher<User>().HashPassword(user, dto.Password);
            await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();
            return ToDto(user);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null) return false;
            user.IsActive = false;
            _repository.Update(user);
            await _repository.SaveChangesAsync();
            return true;
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
