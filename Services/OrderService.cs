using EcommerceAPI.DTOs;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;

namespace EcommerceAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo;
        private readonly EcommerceContext _db;

        public OrderService(
            IOrderRepository orderRepo,
            IProductRepository productRepo,
            EcommerceContext db)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _db = db;
        }

        public async Task<OrderDto?> GetByIdAsync(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return null;
            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderNumber = order.OrderNumber,
                ShippingAddress = order.ShippingAddress,
                Status = order.Status,
                OrderDate = order.OrderDate,
                Items = order.Items.Select(ToItemDto).ToList()
            };
        }

        public async Task<IEnumerable<OrderDto>> GetByUserIdAsync(int userId)
            => (await _orderRepo.GetByUserIdAsync(userId)).Select(ToDto);

        public async Task<OrderPageDto> GetAllAsync(int page = 1, int pageSize = 10)
        {
            if (page < 1 || pageSize is < 1 or > 100) throw new ArgumentException("Pagination invalide.");
            var result = await _orderRepo.GetPagedAsync(page, pageSize);
            return new OrderPageDto
            {
                Items = result.Items.Select(ToDto),
                Page = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task<OrderDto> CreateAsync(CreateOrderDto dto, int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.ShippingAddress)) throw new ArgumentException("L'adresse de livraison est obligatoire.");
            if (dto.Items == null || dto.Items.Count == 0) throw new ArgumentException("Le panier ne peut pas être vide.");

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = $"#{DateTime.UtcNow.Ticks}",
                    ShippingAddress = dto.ShippingAddress,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending
                };

                foreach (var it in dto.Items)
                {
                    var product = await _productRepo.GetByIdAsync(it.ProductId);
                    if (product == null) throw new KeyNotFoundException($"Produit {it.ProductId} introuvable.");
                    if (product.StockQuantity < it.Quantity) throw new InvalidOperationException($"Stock insuffisant pour {product.Name}.");
                    order.Items.Add(new OrderItem
                    {
                        ProductId = it.ProductId,
                        Quantity = it.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * it.Quantity
                    });

                    product.StockQuantity -= it.Quantity;
                    _productRepo.Update(product);
                }

                await _orderRepo.AddAsync(order);
                await _orderRepo.SaveChangesAsync();
                await transaction.CommitAsync();

                return (await GetByIdAsync(order.Id))!;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return false;
            order.Status = dto.Status;
            if (dto.Status == OrderStatus.Shipped) order.ShippedDate = DateTime.UtcNow;
            if (dto.Status == OrderStatus.Delivered) order.DeliveredDate = DateTime.UtcNow;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAsync(int id, int userId, bool isAdmin)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null || (!isAdmin && order.UserId != userId) || order.Status != OrderStatus.Pending) return false;

            foreach (var item in order.Items)
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    _productRepo.Update(product);
                }
            }

            order.Status = OrderStatus.Cancelled;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
            return true;
        }

        private static OrderDto ToDto(Order o) => new()
        {
            Id = o.Id, UserId = o.UserId, OrderNumber = o.OrderNumber,
            ShippingAddress = o.ShippingAddress, Status = o.Status, OrderDate = o.OrderDate,
            Items = o.Items.Select(ToItemDto).ToList()
        };

        private static OrderItemDto ToItemDto(OrderItem i) => new()
        {
            Id = i.Id, ProductId = i.ProductId, Quantity = i.Quantity,
            UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice
        };
    }
}
