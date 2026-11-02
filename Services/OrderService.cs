using EcommerceAPI.DTOs;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Services
{
    public class OrderService : IOrderService
    {
        /// <summary>Frais de livraison fixes, en dinars tunisiens (TND).</summary>
        public const decimal DeliveryFee = 10m;
        public const string Currency = "TND";

        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo;
        private readonly ICouponService _coupons;
        private readonly EcommerceContext _db;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepo,
            IProductRepository productRepo,
            ICouponService coupons,
            EcommerceContext db,
            ILogger<OrderService> logger)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _coupons = coupons;
            _db = db;
            _logger = logger;
        }

        public async Task<OrderDto?> GetByIdAsync(int id)
        {
            var order = await _orderRepo.GetDetailedByIdAsync(id);
            return order == null ? null : ToDto(order);
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
            if (dto.Items.Any(i => i.Quantity < 1)) throw new ArgumentException("Les quantités doivent être supérieures à zéro.");

            // Un même produit (et une même taille) présent deux fois est fusionné en une seule ligne
            var lines = dto.Items
                .GroupBy(i => (i.ProductId, Size: string.IsNullOrWhiteSpace(i.Size) ? null : i.Size.Trim()))
                .Select(g => (g.Key.ProductId, g.Key.Size, Quantity: g.Sum(i => i.Quantity)))
                .ToList();

            var couponLines = new List<(int CategoryId, decimal Total)>();
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = $"#{DateTime.UtcNow.Ticks}",
                    ShippingAddress = dto.ShippingAddress,
                    PaymentMethod = PaymentMethods.CashOnDelivery,
                    DeliveryFee = DeliveryFee,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending
                };

                foreach (var (productId, size, quantity) in lines)
                {
                    var product = await _productRepo.GetByIdAsync(productId);
                    if (product == null) throw new KeyNotFoundException($"Produit {productId} introuvable.");
                    if (product.SellerId == userId) throw new InvalidOperationException($"Vous ne pouvez pas acheter votre propre produit ({product.Name}).");
                    var available = product.SizeList();
                    if (available.Count > 0 && size == null)
                        throw new ArgumentException($"Choisissez une taille pour {product.Name}.");
                    if (size != null && !available.Contains(size))
                        throw new ArgumentException($"La taille « {size} » n'est pas disponible pour {product.Name}.");
                    // Le stock est global au produit (non réparti par taille) ; chaque ligne le décrémente
                    if (product.StockQuantity < quantity) throw new InvalidOperationException($"Stock insuffisant pour {product.Name}.");
                    var unitPrice = product.CurrentPrice(DateTime.UtcNow);
                    order.Items.Add(new OrderItem
                    {
                        ProductId = productId,
                        Size = size,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * quantity
                    });

                    couponLines.Add((product.CategoryId, unitPrice * quantity));
                    product.StockQuantity -= quantity;
                    _productRepo.Update(product);
                }

                // La remise est toujours recalculée ici : on ne fait jamais confiance au montant affiché par le client
                if (!string.IsNullOrWhiteSpace(dto.CouponCode))
                {
                    var subtotal = order.Items.Sum(i => i.TotalPrice);
                    var (coupon, discount) = await _coupons.ResolveAsync(dto.CouponCode, subtotal, couponLines);
                    await _coupons.ConsumeAsync(coupon);
                    order.CouponCode = coupon.Code;
                    order.DiscountAmount = discount;
                }

                await _orderRepo.AddAsync(order);
                await _orderRepo.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Commande {OrderId} créée par l'utilisateur {UserId} : {ItemCount} article(s), code promo {Coupon}, remise {Discount} {Currency}",
                    order.Id, userId, order.Items.Count, order.CouponCode ?? "aucun", order.DiscountAmount, Currency);

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
            var previous = order.Status;
            if (dto.Status is OrderStatus.Shipped or OrderStatus.Delivered && order.DeliveryPersonId == null)
                throw new InvalidOperationException("Affectez d'abord la commande à un livreur.");
            if (dto.Status is OrderStatus.Shipped or OrderStatus.Delivered && order.DeliveryAssignment != DeliveryAssignmentStatus.Accepted)
                throw new InvalidOperationException("Le livreur doit d'abord accepter la livraison.");
            order.Status = dto.Status;
            if (dto.Status == OrderStatus.Shipped) order.ShippedDate = DateTime.UtcNow;
            if (dto.Status == OrderStatus.Delivered) order.DeliveredDate = DateTime.UtcNow;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
            _logger.LogInformation("Commande {OrderId} : statut {Previous} -> {Status}", id, previous, dto.Status);
            return true;
        }

        public async Task<bool> AssignDeliveryAsync(int id, int? deliveryPersonId)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return false;
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Delivered)
                throw new InvalidOperationException("Impossible d'affecter un livreur à une commande annulée ou déjà livrée.");

            if (deliveryPersonId.HasValue)
            {
                var person = await _db.Users.FindAsync(deliveryPersonId.Value);
                if (person == null || person.Role != UserRole.Delivery || !person.IsActive)
                    throw new ArgumentException("Livreur introuvable ou inactif.");
                order.DeliveryPersonId = person.Id;
                order.DeliveryAssignedDate = DateTime.UtcNow;
                order.DeliveryAssignment = DeliveryAssignmentStatus.Pending;
                // Affecter une demande en attente = l'accepter
                if (order.Status == OrderStatus.Pending) order.Status = OrderStatus.Processing;
            }
            else
            {
                if (order.Status == OrderStatus.Shipped)
                    throw new InvalidOperationException("La commande est déjà en cours de livraison.");
                order.DeliveryPersonId = null;
                order.DeliveryAssignedDate = null;
                order.DeliveryAssignment = DeliveryAssignmentStatus.None;
            }

            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
            _logger.LogInformation("Commande {OrderId} affectée au livreur {DeliveryPersonId}", id, deliveryPersonId);
            return true;
        }

        public async Task<IEnumerable<OrderDto>> GetByDeliveryPersonAsync(int deliveryPersonId)
            => (await _orderRepo.GetByDeliveryPersonIdAsync(deliveryPersonId)).Select(ToDto);

        public async Task<bool> UpdateDeliveryStatusAsync(int id, int deliveryPersonId, OrderStatus status)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null || order.DeliveryPersonId != deliveryPersonId) return false;
            if (order.DeliveryAssignment != DeliveryAssignmentStatus.Accepted)
                throw new InvalidOperationException("Acceptez d'abord la livraison.");

            var allowed = (order.Status, status) is (OrderStatus.Processing, OrderStatus.Shipped)
                or (OrderStatus.Shipped, OrderStatus.Delivered);
            if (!allowed)
                throw new InvalidOperationException("Transition de statut non autorisée pour le livreur.");

            order.Status = status;
            if (status == OrderStatus.Shipped) order.ShippedDate = DateTime.UtcNow;
            if (status == OrderStatus.Delivered) order.DeliveredDate = DateTime.UtcNow;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
            _logger.LogInformation("Commande {OrderId} : statut -> {Status} (livreur {DeliveryPersonId})", id, status, deliveryPersonId);
            return true;
        }

        public async Task<bool> RespondToAssignmentAsync(int id, int deliveryPersonId, bool accept)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null || order.DeliveryPersonId != deliveryPersonId) return false;
            if (order.DeliveryAssignment != DeliveryAssignmentStatus.Pending || order.Status is OrderStatus.Cancelled or OrderStatus.Delivered)
                throw new InvalidOperationException("Cette affectation n'attend plus de réponse.");

            order.DeliveryAssignment = accept ? DeliveryAssignmentStatus.Accepted : DeliveryAssignmentStatus.Refused;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
            _logger.LogInformation("Commande {OrderId} : livreur {DeliveryPersonId} a {Answer} l'affectation",
                id, deliveryPersonId, accept ? "accepté" : "refusé");
            return true;
        }

        public async Task<DeliveryStatsDto> GetDeliveryStatsAsync(int deliveryPersonId, int days)
        {
            var since = DateTime.UtcNow.Date.AddDays(-(days - 1));

            var mine = _db.Orders.AsNoTracking()
                .Where(o => o.DeliveryPersonId == deliveryPersonId && o.Status != OrderStatus.Cancelled);

            var delivered = await mine
                .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredDate >= since)
                .Select(o => new { o.DeliveredDate, o.DeliveryFee, o.DiscountAmount, Items = o.Items.Sum(i => i.TotalPrice) })
                .ToListAsync();

            var byDay = delivered.GroupBy(o => o.DeliveredDate.Date)
                .ToDictionary(g => g.Key, g => (Count: g.Count(), Fees: g.Sum(o => o.DeliveryFee)));

            return new DeliveryStatsDto
            {
                Currency = Currency,
                Days = days,
                DeliveredCount = delivered.Count,
                Earnings = delivered.Sum(o => o.DeliveryFee),
                CashCollected = delivered.Sum(o => o.Items - o.DiscountAmount + o.DeliveryFee),
                PendingAssignments = await mine.CountAsync(o => o.DeliveryAssignment == DeliveryAssignmentStatus.Pending),
                InProgress = await mine.CountAsync(o => o.DeliveryAssignment == DeliveryAssignmentStatus.Accepted
                                                        && o.Status != OrderStatus.Delivered),
                Daily = Enumerable.Range(0, days).Select(i => since.AddDays(i)).Select(d => new DeliveryDayDto
                {
                    Date = d,
                    Deliveries = byDay.TryGetValue(d, out var v) ? v.Count : 0,
                    Earnings = byDay.TryGetValue(d, out var v2) ? v2.Fees : 0
                }).ToList()
            };
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

            // Le code promo redevient utilisable
            if (!string.IsNullOrEmpty(order.CouponCode)) await _coupons.ReleaseAsync(order.CouponCode);

            _logger.LogInformation("Commande {OrderId} annulée (demandée par l'utilisateur {UserId})", id, userId);
            return true;
        }

        private static OrderDto ToDto(Order o)
        {
            var subtotal = o.Items.Sum(i => i.TotalPrice);
            return new OrderDto
            {
                Id = o.Id, UserId = o.UserId, OrderNumber = o.OrderNumber,
                CustomerName = o.User == null ? string.Empty : $"{o.User.FirstName} {o.User.LastName}".Trim(),
                CustomerEmail = o.User?.Email ?? string.Empty,
                CustomerPhone = o.User?.PhoneNumber ?? string.Empty,
                DeliveryPersonId = o.DeliveryPersonId,
                DeliveryPersonName = o.DeliveryPerson == null ? null : $"{o.DeliveryPerson.FirstName} {o.DeliveryPerson.LastName}".Trim(),
                DeliveryPersonPhone = o.DeliveryPerson?.PhoneNumber,
                DeliveryAssignment = o.DeliveryAssignment,
                ShippingAddress = o.ShippingAddress, Status = o.Status, OrderDate = o.OrderDate,
                PaymentMethod = o.PaymentMethod, Currency = Currency, DeliveryFee = o.DeliveryFee,
                CouponCode = o.CouponCode, DiscountAmount = o.DiscountAmount,
                Subtotal = subtotal, Total = subtotal - o.DiscountAmount + o.DeliveryFee,
                ShippedDate = o.ShippedDate > DateTime.MinValue ? o.ShippedDate : null,
                DeliveredDate = o.DeliveredDate > DateTime.MinValue ? o.DeliveredDate : null,
                Items = o.Items.Select(ToItemDto).ToList()
            };
        }

        private static OrderItemDto ToItemDto(OrderItem i) => new()
        {
            Id = i.Id, ProductId = i.ProductId, Quantity = i.Quantity, Size = i.Size,
            ProductName = i.Product?.Name ?? "Produit indisponible",
            ProductImageUrl = i.Product?.ImageUrl ?? string.Empty,
            UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice
        };
    }
}
