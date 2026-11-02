using System.ComponentModel.DataAnnotations;
using EcommerceAPI.Models;

namespace EcommerceAPI.DTOs
{
    /// <summary>Affectation d'une commande à un livreur (null = retirer l'affectation).</summary>
    public class AssignDeliveryDto
    {
        public int? DeliveryPersonId { get; set; }
    }

    /// <summary>Le livreur ne peut faire avancer la commande que vers « Shipped » (en route) puis « Delivered ».</summary>
    public class DeliveryStatusDto
    {
        [EnumDataType(typeof(OrderStatus))]
        public OrderStatus Status { get; set; }
    }

    /// <summary>Réponse du livreur à une affectation.</summary>
    public class DeliveryResponseDto
    {
        public bool Accept { get; set; }
    }

    public class DeliveryDayDto
    {
        public DateTime Date { get; set; }
        public int Deliveries { get; set; }
        public decimal Earnings { get; set; }
    }

    /// <summary>Statistiques d'un livreur sur les <c>Days</c> derniers jours (d'après la date de livraison).</summary>
    public class DeliveryStatsDto
    {
        public string Currency { get; set; } = "TND";
        public int Days { get; set; }
        public int DeliveredCount { get; set; }
        /// <summary>Gains du livreur : somme des frais de livraison des commandes livrées.</summary>
        public decimal Earnings { get; set; }
        /// <summary>Espèces encaissées auprès des clients (total des commandes livrées), à reverser à la boutique.</summary>
        public decimal CashCollected { get; set; }
        /// <summary>Affectations en attente de réponse.</summary>
        public int PendingAssignments { get; set; }
        /// <summary>Commandes acceptées, pas encore livrées.</summary>
        public int InProgress { get; set; }
        public List<DeliveryDayDto> Daily { get; set; } = new();
    }

    /// <summary>Création d'un compte livreur par l'admin.</summary>
    public class CreateDeliveryPersonDto
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;
        [RegularExpression(@"^(\d{8})?$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 8 chiffres.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
