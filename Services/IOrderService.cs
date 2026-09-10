using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface IOrderService
    {
        Task<OrderDto?> GetByIdAsync(int id);
        Task<IEnumerable<OrderDto>> GetByUserIdAsync(int userId);
        Task<IEnumerable<OrderDto>> GetAllAsync();
        Task<OrderDto> CreateAsync(CreateOrderDto dto, int userId);
        Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<bool> CancelAsync(int id, int userId, bool isAdmin);
    }
}
