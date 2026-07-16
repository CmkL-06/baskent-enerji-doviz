using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Business.Services.Telegram;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Infrastructure.Telegram;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Telegram;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        private readonly IExchangeTransactionService _exchangeTransactionService;
        private readonly ILogger<TelegramOperatorController> _logger;
        private readonly ITelegramNotificationService _telegramNotificationService;

        public TelegramOperatorController(
            BaskentEnerjiDbContext db,
            ValidationService validationService,
            IConfiguration configuration,
            IExchangeTransactionService exchangeTransactionService,
            ILogger<TelegramOperatorController> logger,
            ITelegramNotificationService telegramNotificationService)
        {
            _db = db;
            _validationService = validationService;
            _configuration = configuration;
            _exchangeTransactionService = exchangeTransactionService;
            _logger = logger;
            _telegramNotificationService = telegramNotificationService;
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
                    t.DeliveryMethod,
                    t.CustomerAddress,
                    t.CustomerPhone,
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
            {
                tx.CompletedAt = DateTime.Now;

                // Şube'ye (gerçek Vault'a) bağlı bir TgDealer ise, işlemi gerçek döviz muhasebesine (Kasa) işle.
                // Harici bayilerde (DealerType.External) mevcut cari hesap akışı (record-entry) hiç etkilenmez.
                var dealer = await _db.TgDealers.FirstOrDefaultAsync(d => d.DealerCode == tx.ReferralCode);
                if (dealer != null && dealer.DealerType == TgDealerType.Branch)
                {
                    if (!Guid.TryParse(dealer.VaultId, out var vaultId))
                        throw new ApiException(HttpStatusCode.InternalServerError,
                            $"Bayi '{dealer.DealerCode}' Şube (Branch) olarak işaretli ama geçerli bir VaultId'si yok.");

                    var currencyCode = TgCurrencyMapper.ToSystemCurrencyCode(tx.Currency);
                    var sourceCurrency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                        .FirstOrDefaultAsync(c => c.CurrencyCode == currencyCode);
                    var tryCurrency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                        .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");

                    if (sourceCurrency == null || tryCurrency == null)
                        throw new ApiException(HttpStatusCode.InternalServerError, $"Para birimi bulunamadı: {currencyCode}");

                    tx.VaultId = vaultId;

                    await _exchangeTransactionService.ProcessExchangeAsync(new System.Collections.Generic.List<rm_exchangetransaction>
                    {
                        new rm_exchangetransaction
                        {
                            VaultId = vaultId,
                            SourceCurrencyId = sourceCurrency.Id,
                            TargetCurrencyId = tryCurrency.Id,
                            SourceAmount = tx.Amount ?? 0,
                            IsBuyingFromCustomer = tx.IsBuy == true,
                            CustomRate = tx.ExchangeRate,
                            Notes = $"Telegram işlemi #{tx.TransactionId} — bayi kodu {tx.ReferralCode}"
                        }
                    });

                    _logger.LogInformation("TG işlemi #{TxId} şube kasasına işlendi (VaultId: {VaultId})", tx.TransactionId, vaultId);
                }
            }

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

            TelegramEventsController.Broadcast("transaction_update", new { transactionId = tx.TransactionId, status = newStatus, referralCode = tx.ReferralCode });

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

            if (tx.CustomerId.HasValue)
                await SendTelegramMessage(tx.CustomerId.Value,
                    $"✅ <b>Ödemeniz ulaştı!</b>\n\n💰 Tutar: {tx.Amount} {tx.Currency ?? "USDT"}\n\nİşleminiz onaylandı, operatör tarafından tamamlanması bekleniyor.");

            TelegramEventsController.Broadcast("transaction_update", new { transactionId = tx.TransactionId, status = tx.Status, cryptoVerified = true });

            return Ok(new { success = true, cryptoVerified = true, cryptoVerifiedAt = tx.CryptoVerifiedAt });
        }

        private Task SendTelegramMessage(long chatId, string text)
            => _telegramNotificationService.SendMessageAsync(chatId, text);
    }

    public class SendChatRequest
    {
        public string Message { get; set; } = "";
    }
}
