using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface IUserService
    {
        Task<UserDto?> GetByIdAsync(int id);
        Task<UserPageDto> GetAllAsync(int page = 1, int pageSize = 10);
        Task<bool> UpdateAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<UserDto>> GetDeliveryPeopleAsync();
        /// <summary>null si l'e-mail existe déjà.</summary>
        Task<UserDto?> CreateDeliveryPersonAsync(CreateDeliveryPersonDto dto);
    }
}
