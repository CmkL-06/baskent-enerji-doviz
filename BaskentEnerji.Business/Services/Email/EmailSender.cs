using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.Email;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var enableMail = _configuration["EmailSettings:EnableMail"];
            if (string.IsNullOrEmpty(enableMail) || !bool.TryParse(enableMail, out var enabled) || !enabled)
                return;

            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var fromAddress = _configuration["EmailSettings:From"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(fromAddress))
                {
                    throw new ApiException(System.Net.HttpStatusCode.InternalServerError, "Email settings are incomplete");
                }

                var portRaw = _configuration["EmailSettings:Port"];
                var port = int.TryParse(portRaw, out var parsedPort) ? parsedPort : 587;

                using var message = new MailMessage(fromAddress, email)
                {
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                using var client = new SmtpClient(smtpServer, port)
                {
                    EnableSsl = true,
                    Credentials = string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(username, password)
                };

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed to {Email}", email);
                throw new ApiException(System.Net.HttpStatusCode.InternalServerError, "Email sending failed: " + ex.Message);
            }
        }
    }
}
