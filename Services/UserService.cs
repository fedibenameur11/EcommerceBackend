using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;

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

        public async Task<IEnumerable<UserDto>> GetAllAsync()
            => (await _repository.GetAllAsync()).Select(ToDto);

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

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null) return false;
            _repository.Remove(user);
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
