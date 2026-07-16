using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.Telegram
{
    public interface ITelegramNotificationService
    {
        Task SendMessageAsync(long chatId, string text);
    }
}
