using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MoneyTransferTurkey.Business.Exceptions;
using MoneyTransferTurkey.Business.Infrastructure.Email;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Elixir Medical", _configuration["EmailSettings:From"]));
            emailMessage.To.Add(new MailboxAddress("Activation", email));
            emailMessage.Subject = subject;
            string test = _configuration["EmailSettings:SmtpServer"];
            string test2 = _configuration["EmailSettings:Port"];

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"], int.Parse(_configuration["EmailSettings:Port"]), SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);
                    await client.SendAsync(emailMessage);
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    throw new ApiException(System.Net.HttpStatusCode.InternalServerError, "Email sending failed" +  ex);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}
