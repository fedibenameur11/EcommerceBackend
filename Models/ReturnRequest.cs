namespace EcommerceAPI.Models
{
    public enum ReturnStatus
    {
        /// <summary>Demande envoyée par le client, en attente de décision.</summary>
        Requested = 0,
        /// <summary>Retour accepté : le client renvoie l'article.</summary>
        Approved = 1,
        Rejected = 2,
        /// <summary>Article reçu en retour (le stock est remis à jour).</summary>
        Received = 3,
        Refunded = 4
    }

    /// <summary>Demande de retour (et de remboursement) portant sur une ligne d'une commande livrée.</summary>
    public class ReturnRequest
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public int UserId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public ReturnStatus Status { get; set; } = ReturnStatus.Requested;
        /// <summary>Message de l'administrateur au client (motif de refus, instructions de renvoi...).</summary>
        public string AdminNote { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Order Order { get; set; } = null!;
        public OrderItem OrderItem { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
