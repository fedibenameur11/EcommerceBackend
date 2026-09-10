using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface IUserService
    {
        Task<UserDto?> GetByIdAsync(int id);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<bool> UpdateAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
