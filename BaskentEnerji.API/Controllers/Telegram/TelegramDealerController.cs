using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Party;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.Telegram
{
    [Route("api/v1/tg/dealer")]
    [ApiController]
    [Authorize]
    public class TelegramDealerController : ControllerBase
    {
        private readonly BaskentEnerjiDbContext _db;
        private readonly ValidationService _validationService;

        public TelegramDealerController(BaskentEnerjiDbContext db, ValidationService validationService)
        {
            _db = db;
            _validationService = validationService;
        }

        private async Task RequireStaff()
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Forbidden, "Staff yetkisi gereklidir.");
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            await RequireStaff();

            var userIdStr = _validationService.GetUserID();
            if (!Guid.TryParse(userIdStr, out var userId))
                throw new ApiException(HttpStatusCode.Unauthorized, "Geçersiz kullanıcı");

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                throw new ApiException(HttpStatusCode.NotFound, "Kullanıcı bulunamadı");

            var dealerCode = user.DealerReferralCode ?? "";

            var tgDealer = await _db.TgDealers
                .FirstOrDefaultAsync(d => d.DealerCode == dealerCode);

            var txs = await _db.TgTransactions
                .Where(t => t.ReferralCode == dealerCode)
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
                    t.IsBuy,
                    t.CreatedAt,
                    t.CompletedAt
                })
                .ToListAsync();

            var completedTxs = txs.Where(t => t.Status?.ToLower() == "completed").ToList();
            var givenTl = completedTxs.Sum(t => (decimal?)t.TlAmount ?? 0);
            var usdtTotal = completedTxs.Where(t => t.Currency?.ToUpper() == "USDT").Sum(t => (decimal?)t.Amount ?? 0);
            var rubTotal = completedTxs.Where(t => t.Currency?.ToUpper() == "RUB" || t.Currency?.ToUpper() == "RUBLE").Sum(t => (decimal?)t.Amount ?? 0);

            var cryptoTxs = txs.Where(t => t.Currency?.ToUpper() == "USDT").ToList();
            var cryptoVerified = completedTxs.Where(t => t.Currency?.ToUpper() == "USDT").Count();

            var dealerName = tgDealer?.DealerName ?? $"{user.Firstname} {user.Lastname}".Trim();
            var dealerBalance = tgDealer?.Balance ?? 0;

            return Ok(new
            {
                balance = dealerBalance,
                given_tl = givenTl,
                usdt = usdtTotal,
                rub = rubTotal,
                dealer_name = dealerName,
                dealer_code = dealerCode,
                staff_name = $"{user.Firstname} {user.Lastname}".Trim(),
                transactions = txs,
                crypto_summary = new
                {
                    total_usdt_tx = cryptoTxs.Count,
                    completed_usdt_tx = cryptoVerified,
                    total_usdt_amount = usdtTotal
                },
                dealer = new
                {
                    Id = tgDealer?.DealerId ?? 0,
                    Name = dealerName,
                    DealerCode = dealerCode,
                    IsActive = tgDealer?.IsActive ?? (user.Rank != Entity.Rank.Banned),
                    Balance = dealerBalance,
                    user.CreatedDate
                }
            });
        }

        [HttpGet("crypto-deposits")]
        public async Task<IActionResult> CryptoDeposits()
        {
            await RequireStaff();

            var userIdStr = _validationService.GetUserID();
            if (!Guid.TryParse(userIdStr, out var userId))
                throw new ApiException(HttpStatusCode.Unauthorized, "Geçersiz kullanıcı");

            var user = await _db.Users.FindAsync(userId);
            var dealerCode = user?.DealerReferralCode ?? "";

            var txIds = await _db.TgTransactions
                .Where(t => t.ReferralCode == dealerCode)
                .Select(t => t.TransactionId)
                .ToListAsync();

            var deposits = await _db.TgCryptoDeposits
                .Where(d => d.TransactionId.HasValue && txIds.Contains(d.TransactionId.Value))
                .OrderByDescending(d => d.DepositTime)
                .Select(d => new
                {
                    Id = d.DepositId,
                    d.TransactionId,
                    d.Txid,
                    d.Amount,
                    d.Network,
                    Address = d.ToAddress,
                    d.Confirmations,
                    d.Status,
                    CreatedAt = d.DepositTime
                })
                .ToListAsync();

            return Ok(new { deposits });
        }

        // ═══════════════════════════════════════════════
        // CARİ HESAP — record-entry (vault'a DOKUNMAZ)
        // ═══════════════════════════════════════════════

        public class RecordEntryRequest
        {
            public string DealerCode { get; set; } = "";
            public int? TransactionId { get; set; }
            public string Currency { get; set; } = "";
            public decimal Amount { get; set; }
            public decimal AmountTry { get; set; }
            public decimal ExchangeRate { get; set; }
            public bool IsBuy { get; set; }
        }

        [HttpPost("record-entry")]
        public async Task<IActionResult> RecordEntry([FromBody] RecordEntryRequest req)
        {
            await RequireStaff();

            if (req.Amount <= 0 || req.AmountTry <= 0)
                throw new ApiException(HttpStatusCode.BadRequest, "Tutar sıfır veya negatif olamaz");
            if (req.ExchangeRate <= 0)
                throw new ApiException(HttpStatusCode.BadRequest, "Kur sıfır veya negatif olamaz");
            if (string.IsNullOrWhiteSpace(req.DealerCode))
                throw new ApiException(HttpStatusCode.BadRequest, "Bayi kodu boş olamaz");

            var dealer = await _db.TgDealers
                .FirstOrDefaultAsync(d => d.DealerCode == req.DealerCode && d.IsActive);
            if (dealer?.PartyId == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Bayi cari hesabı bulunamadı");

            // Duplicate kontrolü: aynı transaction_id ile kayıt varsa reddet
            if (req.TransactionId.HasValue)
            {
                var refNum = req.TransactionId.Value.ToString();
                var exists = await _db.PartyAccountEntries
                    .AnyAsync(e => e.ReferenceNumber == refNum
                        && e.PartyAccount.PartyId == dealer.PartyId
                        && !e.IsReversed);
                if (exists)
                    return Ok(new { duplicate = true, message = "Bu işlem için cari kayıt zaten mevcut" });
            }

            var tryCurrency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (tryCurrency == null)
                throw new ApiException(HttpStatusCode.InternalServerError, "TRY para birimi bulunamadı");

            var account = await _db.PartyAccounts
                .FirstOrDefaultAsync(a => a.PartyId == dealer.PartyId && a.CurrencyId == tryCurrency.Id && a.IsActive);
            if (account == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Bayi TRY hesabı bulunamadı");

            var commission = req.AmountTry * dealer.CommissionRate / 100m;
            var entryType = req.IsBuy ? EntryType.Credit : EntryType.Debit;
            var entryAmount = req.IsBuy
                ? req.AmountTry + commission
                : req.AmountTry - commission;

            if (entryAmount <= 0)
                throw new ApiException(HttpStatusCode.BadRequest, "Komisyon hesabı sonrası tutar sıfır veya negatif olamaz");

            Guid? originalCurrencyId = null;
            var cur = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .FirstOrDefaultAsync(c => c.CurrencyCode == req.Currency.ToUpper());
            if (cur != null) originalCurrencyId = cur.Id;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (entryType == EntryType.Credit)
                {
                    account.Balance -= entryAmount;
                    account.TotalCredits += entryAmount;
                }
                else
                {
                    account.Balance += entryAmount;
                    account.TotalDebits += entryAmount;
                }
                account.TransactionCount++;
                account.LastTransactionDate = DateTime.UtcNow;

                var direction = req.IsBuy ? "Alış" : "Satış";
                var entry = new PartyAccountEntry
                {
                    PartyAccountId = account.Id,
                    EntryNumber = $"PE{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
                    EntryDate = DateTime.UtcNow,
                    Type = entryType,
                    Amount = entryAmount,
                    RunningBalance = account.Balance,
                    Description = $"{req.Currency.ToUpper()} {direction} - {req.Amount} {req.Currency.ToUpper()} @ {req.ExchangeRate:F2}",
                    PaymentStatus = PaymentStatus.Pending,
                    OriginalCurrencyId = originalCurrencyId,
                    OriginalAmount = req.Amount,
                    ExchangeRate = req.ExchangeRate,
                    ReferenceNumber = req.TransactionId?.ToString()
                };

                _db.PartyAccountEntries.Add(entry);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    entryId = entry.Id,
                    entryNumber = entry.EntryNumber,
                    entryType = entryType == EntryType.Credit ? "Credit" : "Debit",
                    amount = entryAmount,
                    commission,
                    runningBalance = account.Balance,
                    description = entry.Description
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ═══════════════════════════════════════════════
        // CARİ HESAP — cari-summary (owner görünümü)
        // ═══════════════════════════════════════════════

        [HttpGet("cari-summary")]
        public async Task<IActionResult> CariSummary()
        {
            await RequireStaff();

            var dealers = await _db.TgDealers
                .Where(d => d.PartyId != null)
                .ToListAsync();

            var partyIds = dealers.Where(d => d.PartyId.HasValue).Select(d => d.PartyId!.Value).ToList();

            var tryCurrency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");

            var accounts = await _db.PartyAccounts
                .Where(a => partyIds.Contains(a.PartyId) && a.IsActive
                    && (tryCurrency == null || a.CurrencyId == tryCurrency.Id))
                .ToListAsync();

            var dealerList = dealers.Select(d =>
            {
                var acc = accounts.FirstOrDefault(a => a.PartyId == d.PartyId);
                var balance = acc?.Balance ?? 0;
                string balanceType = balance < 0 ? "payable" : balance > 0 ? "receivable" : "settled";
                return new
                {
                    dealerCode = d.DealerCode,
                    dealerName = d.DealerName,
                    balance,
                    displayBalance = $"{Math.Abs(balance):N2} TL",
                    balanceType,
                    transactionCount = acc?.TransactionCount ?? 0,
                    commissionRate = d.CommissionRate,
                    totalDebits = acc?.TotalDebits ?? 0,
                    totalCredits = acc?.TotalCredits ?? 0,
                    lastTransaction = acc?.LastTransactionDate,
                    isActive = d.IsActive,
                    partyAccountId = acc?.Id
                };
            }).ToList();

            var totalPayable = dealerList.Where(d => d.balance < 0).Sum(d => Math.Abs(d.balance));
            var totalReceivable = dealerList.Where(d => d.balance > 0).Sum(d => d.balance);

            return Ok(new
            {
                dealers = dealerList,
                totals = new
                {
                    totalPayable,
                    totalReceivable,
                    netPosition = totalReceivable - totalPayable
                }
            });
        }

        // ═══════════════════════════════════════════════
        // CARİ HESAP — cari-entries (ekstre)
        // ═══════════════════════════════════════════════

        [HttpGet("{code}/cari-entries")]
        public async Task<IActionResult> CariEntries(string code)
        {
            await RequireStaff();

            var dealer = await _db.TgDealers.FirstOrDefaultAsync(d => d.DealerCode == code);
            if (dealer?.PartyId == null)
                throw new ApiException(HttpStatusCode.NotFound, "Bayi cari hesabı bulunamadı");

            var tryCurrency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");

            var account = await _db.PartyAccounts
                .FirstOrDefaultAsync(a => a.PartyId == dealer.PartyId && a.IsActive
                    && (tryCurrency == null || a.CurrencyId == tryCurrency.Id));
            if (account == null)
                throw new ApiException(HttpStatusCode.NotFound, "Hesap bulunamadı");

            var entries = await _db.PartyAccountEntries
                .Where(e => e.PartyAccountId == account.Id)
                .OrderByDescending(e => e.EntryDate)
                .Take(200)
                .Select(e => new
                {
                    e.Id,
                    e.EntryNumber,
                    e.EntryDate,
                    type = e.Type == EntryType.Debit ? "Debit" : "Credit",
                    e.Amount,
                    debit = e.Type == EntryType.Debit ? e.Amount : (decimal?)null,
                    credit = e.Type == EntryType.Credit ? e.Amount : (decimal?)null,
                    e.RunningBalance,
                    e.Description,
                    paymentStatus = e.PaymentStatus.ToString(),
                    e.ReferenceNumber,
                    e.OriginalAmount,
                    e.ExchangeRate,
                    e.IsReconciled
                })
                .ToListAsync();

            return Ok(new
            {
                dealerCode = dealer.DealerCode,
                dealerName = dealer.DealerName,
                balance = account.Balance,
                balanceType = account.Balance < 0 ? "payable" : account.Balance > 0 ? "receivable" : "settled",
                entries
            });
        }

        // ═══════════════════════════════════════════════
        // CARİ HESAP — record-payment (ödeme kaydı)
        // ═══════════════════════════════════════════════

        public class RecordPaymentRequest
        {
            public string DealerCode { get; set; } = "";
            public decimal Amount { get; set; }
            public string Description { get; set; } = "";
            public string? PaymentReference { get; set; }
        }

        [HttpPost("record-payment")]
        public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest req)
        {
            await RequireStaff();

            if (req.Amount <= 0)
                throw new ApiException(HttpStatusCode.BadRequest, "Ödeme tutarı sıfır veya negatif olamaz");
            if (string.IsNullOrWhiteSpace(req.DealerCode))
                throw new ApiException(HttpStatusCode.BadRequest, "Bayi kodu boş olamaz");

            var dealer = await _db.TgDealers.FirstOrDefaultAsync(d => d.DealerCode == req.DealerCode);
            if (dealer?.PartyId == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Bayi cari hesabı bulunamadı");

            var tryCurrency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (tryCurrency == null)
                throw new ApiException(HttpStatusCode.InternalServerError, "TRY bulunamadı");

            var account = await _db.PartyAccounts
                .FirstOrDefaultAsync(a => a.PartyId == dealer.PartyId && a.CurrencyId == tryCurrency.Id && a.IsActive);
            if (account == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Hesap bulunamadı");

            if (account.Balance == 0)
                throw new ApiException(HttpStatusCode.BadRequest, "Bakiye sıfır — ödeme kaydedilemez");

            var absBalance = Math.Abs(account.Balance);
            if (req.Amount > absBalance)
                throw new ApiException(HttpStatusCode.BadRequest,
                    $"Ödeme tutarı ({req.Amount:N2}) bakiyeyi ({absBalance:N2}) aşamaz");

            var entryType = account.Balance < 0 ? EntryType.Debit : EntryType.Credit;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (entryType == EntryType.Debit)
                {
                    account.Balance += req.Amount;
                    account.TotalDebits += req.Amount;
                }
                else
                {
                    account.Balance -= req.Amount;
                    account.TotalCredits += req.Amount;
                }
                account.TransactionCount++;
                account.LastTransactionDate = DateTime.UtcNow;

                var entry = new PartyAccountEntry
                {
                    PartyAccountId = account.Id,
                    EntryNumber = $"PE{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
                    EntryDate = DateTime.UtcNow,
                    Type = entryType,
                    Amount = req.Amount,
                    RunningBalance = account.Balance,
                    Description = $"Ödeme: {req.Description}",
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentDate = DateTime.UtcNow,
                    PaymentReference = req.PaymentReference ?? ""
                };

                _db.PartyAccountEntries.Add(entry);

                var pendingEntries = await _db.PartyAccountEntries
                    .Where(e => e.PartyAccountId == account.Id && e.PaymentStatus == PaymentStatus.Pending && !e.IsReversed)
                    .OrderBy(e => e.EntryDate)
                    .ToListAsync();

                var remaining = req.Amount;
                foreach (var pe in pendingEntries)
                {
                    if (remaining <= 0) break;
                    if (remaining >= pe.Amount)
                    {
                        pe.PaymentStatus = PaymentStatus.Paid;
                        pe.PaymentDate = DateTime.UtcNow;
                        pe.PaymentReference = req.PaymentReference ?? "";
                        remaining -= pe.Amount;
                    }
                    else
                    {
                        pe.PaymentStatus = PaymentStatus.PartiallyPaid;
                        remaining = 0;
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    entryId = entry.Id,
                    entryType = entryType == EntryType.Debit ? "Debit" : "Credit",
                    amount = req.Amount,
                    runningBalance = account.Balance,
                    settledCount = pendingEntries.Count(e => e.PaymentStatus == PaymentStatus.Paid)
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
