using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.Email;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;
        private static readonly HttpClient _http = new HttpClient();

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var enableMail = _configuration["Smtp:EnableMail"];
            if (string.IsNullOrEmpty(enableMail) || !bool.TryParse(enableMail, out var enabled) || !enabled)
                return;

            try
            {
                var apiKey = _configuration["Smtp:ApiKey"];
                var fromAddress = _configuration["Smtp:From"];
                var fromName = _configuration["Smtp:FromName"] ?? "Baskent Enerji";

                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new ApiException(System.Net.HttpStatusCode.InternalServerError, "Smtp:ApiKey is not configured");

                if (string.IsNullOrWhiteSpace(fromAddress))
                    throw new ApiException(System.Net.HttpStatusCode.InternalServerError, "Smtp:From is not configured");

                var payload = new
                {
                    sender = new { email = fromAddress, name = fromName },
                    to = new[] { new { email = email } },
                    subject = subject,
                    htmlContent = htmlMessage
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                request.Headers.Add("api-key", apiKey);
                request.Content = JsonContent.Create(payload);

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Brevo API error {StatusCode}: {Body}", (int)response.StatusCode, body);
                    throw new ApiException(System.Net.HttpStatusCode.InternalServerError, $"Brevo API error {(int)response.StatusCode}: {body}");
                }

                _logger.LogInformation("Email sent via Brevo API to {Email}", email);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed to {Email}", email);
                throw new ApiException(System.Net.HttpStatusCode.InternalServerError, "Email sending failed: " + ex.Message);
            }
        }
    }
}
