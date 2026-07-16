using BaskentEnerji.Business.Infrastructure.Telegram;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Telegram
{
    /// <summary>
    /// TelegramOperatorController.SendTelegramMessage ile aynı mantık (HTTP POST ile Bot API),
    /// ama controller'a bağımlı değil — AlertService gibi Business katmanındaki servisler de
    /// kullanabilsin diye buraya çıkarıldı.
    /// </summary>
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly IConfiguration _configuration;

        public TelegramNotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendMessageAsync(long chatId, string text)
        {
            var botToken = _configuration["Telegram:MainBotToken"];
            if (string.IsNullOrEmpty(botToken)) return;

            try
            {
                using var client = new HttpClient();
                var payload = JsonSerializer.Serialize(new { chat_id = chatId, text, parse_mode = "HTML" });
                await client.PostAsync(
                    $"https://api.telegram.org/bot{botToken}/sendMessage",
                    new StringContent(payload, Encoding.UTF8, "application/json"));
            }
            catch { }
        }
    }
}
