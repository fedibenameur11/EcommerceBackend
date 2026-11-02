using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class RegisterDto
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
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;
        /// <summary>true = créer un compte vendeur (Seller), sinon client (Customer).</summary>
        public bool IsSeller { get; set; }
        /// <summary>true = créer un compte livreur (Delivery) ; incompatible avec IsSeller, téléphone obligatoire.</summary>
        public bool IsDelivery { get; set; }
    }
}
