using System;
using System.Collections.Generic;

namespace EcommerceAPI.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Foreign Key
        public int UserId { get; set; }

        public string OrderNumber { get; set; } = string.Empty; // ex: "#1025"
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string ShippingAddress { get; set; } = string.Empty;
        /// <summary>Seul mode de paiement : espèces à la livraison.</summary>
        public string PaymentMethod { get; set; } = PaymentMethods.CashOnDelivery;
        public decimal DeliveryFee { get; set; }
        /// <summary>Code promo appliqué à la commande (null = aucun) et remise correspondante en TND.</summary>
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime ShippedDate { get; set; } = DateTime.MinValue;
        public DateTime DeliveredDate { get; set; } = DateTime.MinValue;

        /// <summary>Livreur affecté par l'admin (null = pas encore affectée).</summary>
        public int? DeliveryPersonId { get; set; }
        public DateTime? DeliveryAssignedDate { get; set; }
        /// <summary>Réponse du livreur à l'affectation : en attente, acceptée ou refusée.</summary>
        public DeliveryAssignmentStatus DeliveryAssignment { get; set; } = DeliveryAssignmentStatus.None;

        // Relations
        public User User { get; set; } = null!;
        public User? DeliveryPerson { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public static class PaymentMethods
    {
        public const string CashOnDelivery = "CashOnDelivery";
    }

    public enum DeliveryAssignmentStatus
    {
        /// <summary>Aucun livreur affecté.</summary>
        None = 0,
        /// <summary>Affectée par l'admin, le livreur n'a pas encore répondu.</summary>
        Pending = 1,
        Accepted = 2,
        /// <summary>Refusée par le livreur : l'admin doit choisir un autre livreur.</summary>
        Refused = 3
    }

    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }

}