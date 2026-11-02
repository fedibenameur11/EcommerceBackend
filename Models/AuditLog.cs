namespace EcommerceAPI.Models
{
    /// <summary>Trace d'une modification de données : qui a changé quoi, et quand.</summary>
    public class AuditLog
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int? UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        /// <summary>Create, Update ou Delete.</summary>
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        /// <summary>JSON : { "Propriété": { "old": ..., "new": ... } }</summary>
        public string Changes { get; set; } = "{}";
        public string IpAddress { get; set; } = string.Empty;
    }
}
