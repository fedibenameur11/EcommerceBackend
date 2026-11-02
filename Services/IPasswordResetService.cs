namespace EcommerceAPI.Services
{
    public interface IPasswordResetService
    {
        /// <summary>Envoie un code à 6 chiffres par e-mail (sans rien révéler si le compte n'existe pas).</summary>
        Task RequestCodeAsync(string email);

        /// <summary>Vérifie le code et renvoie un jeton à usage unique pour définir le nouveau mot de passe.</summary>
        Task<string> VerifyCodeAsync(string email, string code);

        Task ResetPasswordAsync(string email, string resetToken, string newPassword);
    }
}
