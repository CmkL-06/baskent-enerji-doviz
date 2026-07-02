using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Telegram;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BaskentEnerji.API.Controllers.Telegram
{
    [Route("api/v1/tg/operator")]
    [ApiController]
    [Authorize]
    public class TelegramOperatorController : ControllerBase
    {
        private readonly BaskentEnerjiDbContext _db;
        private readonly ValidationService _validationService;
        private readonly IConfiguration _configuration;

        public TelegramOperatorController(BaskentEnerjiDbContext db, ValidationService validationService, IConfiguration configuration)
        {
            _db = db;
            _validationService = validationService;
            _configuration = configuration;
        }

        private async Task RequireStaff()
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Forbidden, "Staff yetkisi gereklidir.");
        }

        private async Task<long?> GetCurrentTelegramOperatorId()
        {
            var userIdStr = _validationService.GetUserID();
            if (!Guid.TryParse(userIdStr, out var userId)) return null;
            var user = await _db.Users.FindAsync(userId);
            return user?.TelegramOperatorId;
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> Transactions()
        {
            await RequireStaff();

            var isAdmin = await _validationService.IsAdminAsync();
            var tgOpId = await GetCurrentTelegramOperatorId();

            IQueryable<TgTransaction> query = _db.TgTransactions
                .Include(t => t.Customer);

            if (!isAdmin && tgOpId.HasValue)
                query = query.Where(t => t.AssignedOperatorId == tgOpId.Value);

            var txs = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    Id = t.TransactionId,
                    t.CustomerId,
                    t.Currency,
                    t.Amount,
                    t.ExchangeRate,
                    TlAmount = t.TryAmount,
                    t.Status,
                    t.ReferralCode,
                    OperatorId = t.AssignedOperatorId,
                    t.IsBuy,
                    t.CreatedAt,
                    t.CompletedAt,
                    t.CompletionCode,
                    t.Txid,
                    t.CryptoVerified,
                    t.CryptoVerifiedAt,
                    CustomerName = t.Customer != null ? t.Customer.FirstName : null,
                    CustomerUsername = t.Customer != null ? t.Customer.Username : null
                })
                .ToListAsync();

            return Ok(new { transactions = txs });
        }

        [HttpGet("chat/{txId}")]
        public async Task<IActionResult> GetChat(int txId)
        {
            await RequireStaff();

            var messages = await _db.TgMessages
                .Where(m => m.TransactionId == txId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    Id = m.MessageId,
                    m.TransactionId,
                    SenderId = m.SenderId,
                    m.SenderType,
                    Message = m.MessageText,
                    m.FileUrl,
                    m.FileType,
                    m.CreatedAt
                })
                .ToListAsync();

            return Ok(new { messages });
        }

        [HttpPost("chat/{txId}")]
        public async Task<IActionResult> SendChat(int txId, [FromBody] SendChatRequest req)
        {
            await RequireStaff();

            if (string.IsNullOrWhiteSpace(req.Message))
                throw new ApiException(HttpStatusCode.BadRequest, "Mesaj boş olamaz");

            var tgOpId = await GetCurrentTelegramOperatorId();

            var msg = new TgMessage
            {
                TransactionId = txId,
                SenderId = tgOpId ?? 0,
                SenderType = "operator",
                MessageText = req.Message,
                CreatedAt = DateTime.Now
            };
            _db.TgMessages.Add(msg);
            await _db.SaveChangesAsync();

            var tx = await _db.TgTransactions.FindAsync(txId);
            if (tx?.CustomerId != null)
            {
                var userIdStr = _validationService.GetUserID();
                Guid.TryParse(userIdStr, out var userId);
                var user = await _db.Users.FindAsync(userId);
                var opName = user != null ? user.Firstname : "Operatör";
                await SendTelegramMessage(tx.CustomerId.Value, $"💬 <b>{opName}:</b> {req.Message}");
            }

            return Ok(new { success = true });
        }

        [HttpPost("transaction/{txId}/{txAction}")]
        public async Task<IActionResult> TransactionAction(int txId, string txAction)
        {
            await RequireStaff();

            var allowedActions = new System.Collections.Generic.Dictionary<string, string>
            {
                ["approve"] = "approved",
                ["reject"] = "rejected",
                ["complete"] = "completed",
                ["cancel"] = "cancelled"
            };

            if (!allowedActions.TryGetValue(txAction, out var newStatus))
                throw new ApiException(HttpStatusCode.BadRequest, "Geçersiz işlem");

            var tx = await _db.TgTransactions
                .Include(t => t.Customer)
                .FirstOrDefaultAsync(t => t.TransactionId == txId);

            if (tx == null)
                throw new ApiException(HttpStatusCode.NotFound, "İşlem bulunamadı");

            var tgOpId = await GetCurrentTelegramOperatorId();
            tx.Status = newStatus;
            tx.AssignedOperatorId = tgOpId;

            if (txAction == "complete")
                tx.CompletedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            if (tx.CustomerId.HasValue)
            {
                var message = txAction switch
                {
                    "complete" => $"✅ <b>İşleminiz tamamlandı!</b>\n\n💰 Tutar: {tx.Amount} {tx.Currency ?? "USDT"}\n🔑 Tamamlama Kodu: <code>{tx.CompletionCode ?? "—"}</code>\n\nBizi tercih ettiğiniz için teşekkürler!",
                    "cancel" => $"❌ <b>İşleminiz iptal edildi.</b>\n\n💰 Tutar: {tx.Amount} {tx.Currency ?? "USDT"}\n\nSorularınız için /start ile yeni işlem başlatabilirsiniz.",
                    "reject" => $"⚠️ <b>İşleminiz reddedildi.</b>\n\n💰 Tutar: {tx.Amount} {tx.Currency ?? "USDT"}\n\nDetaylı bilgi için /start ile iletişime geçebilirsiniz.",
                    _ => null
                };
                if (message != null)
                    await SendTelegramMessage(tx.CustomerId.Value, message);
            }

            return Ok(new { success = true, status = newStatus });
        }

        [HttpPost("transaction/{txId}/verify-crypto")]
        public async Task<IActionResult> VerifyCrypto(int txId)
        {
            await RequireStaff();

            var tx = await _db.TgTransactions.FindAsync(txId);
            if (tx == null)
                throw new ApiException(HttpStatusCode.NotFound, "İşlem bulunamadı");

            if (string.IsNullOrEmpty(tx.Txid))
                throw new ApiException(HttpStatusCode.BadRequest, "Bu işlemde TXID bilgisi yok");

            tx.CryptoVerified = true;
            tx.CryptoVerifiedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, cryptoVerified = true, cryptoVerifiedAt = tx.CryptoVerifiedAt });
        }

        private async Task SendTelegramMessage(long chatId, string text)
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

    public class SendChatRequest
    {
        public string Message { get; set; } = "";
    }
}
