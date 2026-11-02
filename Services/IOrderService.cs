using EcommerceAPI.DTOs;
using EcommerceAPI.Models;

namespace EcommerceAPI.Services
{
    public interface IOrderService
    {
        Task<OrderDto?> GetByIdAsync(int id);
        Task<IEnumerable<OrderDto>> GetByUserIdAsync(int userId);
        Task<OrderPageDto> GetAllAsync(int page = 1, int pageSize = 10);
        Task<OrderDto> CreateAsync(CreateOrderDto dto, int userId);
        Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
        /// <summary>Affecte (ou retire) un livreur. Affecter une commande en attente l'accepte (statut « Processing »).</summary>
        Task<bool> AssignDeliveryAsync(int id, int? deliveryPersonId);
        Task<IEnumerable<OrderDto>> GetByDeliveryPersonAsync(int deliveryPersonId);
        /// <summary>Le livreur fait avancer sa commande : Processing -> Shipped -> Delivered.</summary>
        Task<bool> UpdateDeliveryStatusAsync(int id, int deliveryPersonId, OrderStatus status);
        /// <summary>Le livreur accepte ou refuse une affectation en attente.</summary>
        Task<bool> RespondToAssignmentAsync(int id, int deliveryPersonId, bool accept);
        Task<DeliveryStatsDto> GetDeliveryStatsAsync(int deliveryPersonId, int days);
        Task<bool> CancelAsync(int id, int userId, bool isAdmin);
    }
}
