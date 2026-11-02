using EcommerceAPI.Models;

namespace EcommerceAPI.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        /// <summary>Lecture seule : commande avec client et produits (pour l'affichage).</summary>
        Task<Order?> GetDetailedByIdAsync(int id);
        Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
        /// <summary>Commandes (non annulées) affectées à un livreur.</summary>
        Task<IEnumerable<Order>> GetByDeliveryPersonIdAsync(int deliveryPersonId);
        Task<IEnumerable<Order>> GetAllAsync();
        Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(Order order);
        void Update(Order order);
        Task<int> SaveChangesAsync();
    }
}
