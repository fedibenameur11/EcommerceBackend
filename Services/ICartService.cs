using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(int userId);
        Task<CartDto> AddItemAsync(int userId, CartItemDto item);
        Task<CartDto?> UpdateItemQuantityAsync(int userId, int productId, int quantity);
        Task<bool> RemoveItemAsync(int userId, int productId);
    }
}