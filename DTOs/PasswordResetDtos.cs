using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "L'adresse e-mail est obligatoire."),
         EmailAddress(ErrorMessage = "L'adresse e-mail est invalide."),
         MaxLength(255)]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyResetCodeDto
    {
        [Required(ErrorMessage = "L'adresse e-mail est obligatoire."),
         EmailAddress(ErrorMessage = "L'adresse e-mail est invalide."),
         MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code de vérification est obligatoire."),
         RegularExpression(@"^\d{6}$", ErrorMessage = "Le code doit contenir 6 chiffres.")]
        public string Code { get; set; } = string.Empty;
    }

    public class VerifyResetCodeResponseDto
    {
        public string ResetToken { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "L'adresse e-mail est obligatoire."),
         EmailAddress(ErrorMessage = "L'adresse e-mail est invalide."),
         MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le jeton de réinitialisation est obligatoire.")]
        public string ResetToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire."),
         MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
