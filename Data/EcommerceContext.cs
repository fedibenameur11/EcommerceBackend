using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Models;

namespace EcommerceAPI.Data
{
    public class EcommerceContext : DbContext
    {
        public EcommerceContext(DbContextOptions<EcommerceContext> options)
            : base(options)
        {
        }

        // DbSets for each model
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // User ? Cart (one-to-one relationship)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<Cart>(c => c.UserId);

            // User ? Orders (one-to-many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId);

            // Livreur -> commandes affectées (optionnel)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.DeliveryPerson)
                .WithMany(u => u.DeliveryOrders)
                .HasForeignKey(o => o.DeliveryPersonId)
                .OnDelete(DeleteBehavior.SetNull);

            // Category ? Products (one-to-many)
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId);

            // Product ? OrderItems (one-to-many)
            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId);

            // Product ? CartItems (one-to-many)
            modelBuilder.Entity<Product>()
                .HasMany(p => p.CartItems)
                .WithOne(ci => ci.Product)
                .HasForeignKey(ci => ci.ProductId);

            // Order ? OrderItems (one-to-many)
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId);

            // Cart ? CartItems (one-to-many)
            // Seller -> Products (one-to-many, optional)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Seller)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Reviews: one review per user and product
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product).WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId);
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User).WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId);
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.ProductId, r.UserId })
                .IsUnique();

            // Wishlist: a product appears once per user
            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.Product).WithMany(p => p.WishlistItems)
                .HasForeignKey(w => w.ProductId);
            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.User).WithMany(u => u.WishlistItems)
                .HasForeignKey(w => w.UserId);
            modelBuilder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique();

            // Galerie : images supplémentaires d'un produit
            modelBuilder.Entity<ProductImage>()
                .HasOne(i => i.Product).WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Coupons : code unique
            modelBuilder.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
            modelBuilder.Entity<Coupon>().HasOne(c => c.Category).WithMany().HasForeignKey(c => c.CategoryId).OnDelete(DeleteBehavior.SetNull);

            // Retours : une demande porte sur une ligne de commande
            modelBuilder.Entity<ReturnRequest>().Property(r => r.RefundAmount).HasPrecision(18, 3);
            modelBuilder.Entity<ReturnRequest>().HasOne(r => r.Order).WithMany().HasForeignKey(r => r.OrderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ReturnRequest>().HasOne(r => r.OrderItem).WithMany().HasForeignKey(r => r.OrderItemId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ReturnRequest>().HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ReturnRequest>().HasIndex(r => r.OrderItemId);
            modelBuilder.Entity<Coupon>().Property(c => c.Value).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(c => c.MinOrderAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.DiscountAmount).HasPrecision(18, 2);

            // Journal d'audit : recherche par entité et par date
            modelBuilder.Entity<AuditLog>().HasIndex(a => new { a.EntityName, a.EntityId });
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.Timestamp);

            modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId);
        }
    }
}