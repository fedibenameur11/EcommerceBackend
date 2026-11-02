using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class UpdateUserDto
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        [RegularExpression(@"^(\d{8})?$", ErrorMessage = "Le numéro de téléphone doit contenir exactement 8 chiffres.")]
        public string PhoneNumber { get; set; } = string.Empty;
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;
    }
}
