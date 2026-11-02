using System.Net;
using System.Security.Cryptography;
using System.Text;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace EcommerceAPI.Services
{
    /// <summary>
    /// Réinitialisation du mot de passe en 3 étapes : code envoyé par e-mail, vérification du code,
    /// puis définition du nouveau mot de passe avec un jeton à usage unique.
    /// Les codes et jetons sont gardés en mémoire (hachés) et expirent au bout de 10 minutes.
    /// </summary>
    public class PasswordResetService : IPasswordResetService
    {
        private const int CodeLifetimeMinutes = 10;
        private const int MaxAttempts = 5;
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

        private sealed class CodeEntry
        {
            public required string Hash { get; init; }
            public required DateTime SentAtUtc { get; init; }
            public int Attempts;
        }

        private readonly IUserRepository _users;
        private readonly IMemoryCache _cache;
        private readonly IEmailSender _email;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PasswordResetService> _logger;
        private readonly PasswordHasher<User> _hasher = new();

        public PasswordResetService(
            IUserRepository users,
            IMemoryCache cache,
            IEmailSender email,
            IConfiguration configuration,
            ILogger<PasswordResetService> logger)
        {
            _users = users;
            _cache = cache;
            _email = email;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task RequestCodeAsync(string email)
        {
            email = Normalize(email);
            var user = await _users.GetByEmailAsync(email);
            // Même réponse que le compte existe ou non : on ne révèle pas les adresses inscrites.
            if (user == null || !user.IsActive) return;

            if (_cache.TryGetValue(CodeKey(email), out CodeEntry? existing) && existing != null
                && DateTime.UtcNow - existing.SentAtUtc < ResendCooldown)
                return;

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _cache.Set(CodeKey(email), new CodeEntry { Hash = HashCode(email, code), SentAtUtc = DateTime.UtcNow },
                TimeSpan.FromMinutes(CodeLifetimeMinutes));
            _cache.Remove(TokenKey(email));

            try
            {
                await _email.SendAsync(user.Email, "Votre code de réinitialisation de mot de passe", BuildEmail(user.FirstName, code));
            }
            catch
            {
                _cache.Remove(CodeKey(email));
                throw;
            }
        }

        public Task<string> VerifyCodeAsync(string email, string code)
        {
            email = Normalize(email);
            if (!_cache.TryGetValue(CodeKey(email), out CodeEntry? entry) || entry == null)
                throw new ArgumentException("Code invalide ou expiré.");

            if (Interlocked.Increment(ref entry.Attempts) > MaxAttempts)
            {
                _cache.Remove(CodeKey(email));
                throw new ArgumentException("Trop de tentatives. Demandez un nouveau code.");
            }

            if (!FixedTimeEquals(entry.Hash, HashCode(email, code.Trim())))
                throw new ArgumentException("Code invalide ou expiré.");

            _cache.Remove(CodeKey(email));
            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            _cache.Set(TokenKey(email), HashToken(token), TimeSpan.FromMinutes(CodeLifetimeMinutes));
            return Task.FromResult(token);
        }

        public async Task ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            email = Normalize(email);
            if (!_cache.TryGetValue(TokenKey(email), out string? storedHash) || storedHash == null
                || !FixedTimeEquals(storedHash, HashToken(resetToken)))
                throw new ArgumentException("La demande de réinitialisation est invalide ou a expiré.");

            var user = await _users.GetByEmailAsync(email);
            if (user == null || !user.IsActive)
                throw new ArgumentException("La demande de réinitialisation est invalide ou a expiré.");

            user.Password = _hasher.HashPassword(user, newPassword);
            _users.Update(user);
            await _users.SaveChangesAsync();

            _cache.Remove(TokenKey(email));
            _cache.Remove(CodeKey(email));
            _logger.LogInformation("Mot de passe réinitialisé pour l'utilisateur {UserId}.", user.Id);
        }

        private static string Normalize(string email) => email.Trim().ToLowerInvariant();
        private static string CodeKey(string email) => $"pwdreset:code:{email}";
        private static string TokenKey(string email) => $"pwdreset:token:{email}";

        private string HashCode(string email, string code)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "reset-key");
            return Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{email}:{code}")));
        }

        private static string HashToken(string token)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private static bool FixedTimeEquals(string a, string b)
            => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

        private static string BuildEmail(string firstName, string code) =>
            "<div style=\"font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:24px;color:#111\">"
            + "<h2 style=\"margin-top:0\">Réinitialisation du mot de passe</h2>"
            + $"<p>Bonjour {WebUtility.HtmlEncode(firstName)},</p>"
            + "<p>Voici votre code de vérification :</p>"
            + "<p style=\"font-size:32px;font-weight:bold;letter-spacing:8px;background:#f3f4f6;padding:16px;text-align:center;border-radius:8px\">"
            + code + "</p>"
            + $"<p>Ce code est valable {CodeLifetimeMinutes} minutes. Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet e-mail.</p>"
            + "<p style=\"color:#6b7280;font-size:12px\">TounsiBuy</p>"
            + "</div>";
    }
}
