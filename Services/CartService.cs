using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;
        private readonly IProductRepository _productRepo;
        public CartService(ICartRepository cartRepo, IProductRepository productRepo)
        {
            _cartRepo = cartRepo;
            _productRepo = productRepo;
        }

        public async Task<CartDto> GetCartAsync(int userId)
        {
            var cart = await _cartRepo.GetByUserIdAsync(userId);
            if (cart == null) return new CartDto { UserId = userId };
            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Items = cart.Items.Select(i => new CartItemDto { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
            };
        }

        public async Task<CartDto> AddItemAsync(int userId, CartItemDto item)
        {
            if (item.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(item.Quantity));
            var product = await _productRepo.GetByIdAsync(item.ProductId);
            if (product == null) throw new KeyNotFoundException("Produit introuvable.");
            var cart = await _cartRepo.GetByUserIdAsync(userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await _cartRepo.AddAsync(cart);
            }

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
                if (existing.Quantity > product.StockQuantity)
                    throw new InvalidOperationException("La quantité demandée dépasse le stock disponible.");
            }
            else
            {
                if (item.Quantity > product.StockQuantity)
                    throw new InvalidOperationException("La quantité demandée dépasse le stock disponible.");
                cart.Items.Add(new CartItem { ProductId = item.ProductId, Quantity = item.Quantity, UnitPrice = product.Price });
            }

            _cartRepo.Update(cart);
            await _cartRepo.SaveChangesAsync();

            return await GetCartAsync(userId);
        }

        public async Task<CartDto?> UpdateItemQuantityAsync(int userId, int productId, int quantity)
        {
            var cart = await _cartRepo.GetByUserIdAsync(userId);
            if (cart == null) return null;
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return null;
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "La quantité doit être supérieure à zéro.");
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null) throw new KeyNotFoundException("Produit introuvable.");
            if (quantity > product.StockQuantity)
                throw new InvalidOperationException("La quantité demandée dépasse le stock disponible.");
            item.Quantity = quantity;

            _cartRepo.Update(cart);
            await _cartRepo.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<bool> RemoveItemAsync(int userId, int productId)
        {
            var cart = await _cartRepo.GetByUserIdAsync(userId);
            if (cart == null) return false;
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return false;
            cart.Items.Remove(item);
            _cartRepo.Update(cart);
            await _cartRepo.SaveChangesAsync();
            return true;
        }
    }
}