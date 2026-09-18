using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface IOrderService
    {
        Task<OrderDto?> GetByIdAsync(int id);
        Task<IEnumerable<OrderDto>> GetByUserIdAsync(int userId);
        Task<OrderPageDto> GetAllAsync(int page = 1, int pageSize = 10);
        Task<OrderDto> CreateAsync(CreateOrderDto dto, int userId);
        Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<bool> CancelAsync(int id, int userId, bool isAdmin);
    }
}
