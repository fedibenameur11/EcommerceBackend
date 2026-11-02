using System.Net;
using System.Net.Mail;

namespace EcommerceAPI.Services
{
    /// <summary>
    /// Envoie les e-mails via SMTP (section "Smtp" de la configuration).
    /// En développement, si aucun serveur SMTP n'est configuré, le message est simplement écrit dans les logs.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, IHostEnvironment environment, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var smtp = _configuration.GetSection("Smtp");
            var host = smtp["Host"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(smtp["User"]))
            {
                if (_environment.IsDevelopment())
                {
                    _logger.LogWarning("SMTP non configuré (Smtp:Host / Smtp:User / Smtp:Password) : l'e-mail n'est PAS envoyé, voici son contenu. E-mail pour {To} - {Subject} :\n{Body}", to, subject, body);
                    return;
                }
                throw new InvalidOperationException("Le service d'envoi d'e-mails n'est pas configuré (Smtp:Host, Smtp:User, Smtp:Password).");
            }

            var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
            var enableSsl = !bool.TryParse(smtp["EnableSsl"], out var ssl) || ssl;
            var user = smtp["User"];
            var password = smtp["Password"];
            var from = smtp["From"];
            if (string.IsNullOrWhiteSpace(from)) from = user;
            if (string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("L'adresse d'expédition des e-mails n'est pas configurée.");

            using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
            if (!string.IsNullOrWhiteSpace(user))
                client.Credentials = new NetworkCredential(user, password);

            using var message = new MailMessage(new MailAddress(from, smtp["FromName"] ?? "TounsiBuy"), new MailAddress(to))
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec de l'envoi de l'e-mail à {To}.", to);
                throw new InvalidOperationException("Impossible d'envoyer l'e-mail pour le moment. Veuillez réessayer plus tard.");
            }
        }
    }
}
