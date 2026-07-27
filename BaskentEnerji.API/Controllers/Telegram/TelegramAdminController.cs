using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Party;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Telegram;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Telegram;
using BaskentEnerji.Business.Infrastructure.User;
using BaskentEnerji.Entity.Modals.RequestModals.User;
using System;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.Telegram
{
    [Route("api/v1/tg/admin")]
    [ApiController]
    [Authorize]
    public class TelegramAdminController : ControllerBase
    {
        private readonly BaskentEnerjiDbContext _db;
        private readonly ValidationService _validationService;
        private readonly IPartyAccountService _partyAccountService;
        private readonly IExchangeRateService _exchangeRateService;

        public TelegramAdminController(BaskentEnerjiDbContext db, ValidationService validationService,
            IPartyAccountService partyAccountService, IExchangeRateService exchangeRateService)
        {
            _db = db;
            _validationService = validationService;
            _partyAccountService = partyAccountService;
            _exchangeRateService = exchangeRateService;
        }

        private async Task RequireAdmin()
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Admin yetkisi gereklidir.");
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            await RequireAdmin();

            var totalTx = await _db.TgTransactions.CountAsync();
            var completedTx = _db.TgTransactions.Where(t => t.Status == "completed");
            var totalTl = await completedTx.SumAsync(t => (decimal?)t.TryAmount ?? 0);
            var totalUsdt = await completedTx.Where(t => t.Currency == "USDT").SumAsync(t => (decimal?)t.Amount ?? 0);
            var totalRub = await completedTx.Where(t => t.Currency == "RUBLE" || t.Currency == "RUB").SumAsync(t => (decimal?)t.Amount ?? 0);

            var totalDealerBalance = await _db.TgDealers.Where(d => d.IsActive).SumAsync(d => (decimal?)d.Balance ?? 0);
            var totalCustomers = await _db.TgCustomers.CountAsync();
            var pendingTx = await _db.TgTransactions.CountAsync(t => t.Status == "pending" || t.Status == "processing");
            var completedTxCount = await completedTx.CountAsync();

            var recentTx = await _db.TgTransactions
                .Include(t => t.Customer)
                .OrderByDescending(t => t.CreatedAt)
                .Take(15)
                .Select(t => new
                {
                    t.TransactionId,
                    t.Currency,
                    t.Amount,
                    TlAmount = t.TryAmount,
                    t.ExchangeRate,
                    t.Status,
                    t.IsBuy,
                    t.CreatedAt,
                    t.CompletedAt,
                    CustomerName = t.Customer != null ? t.Customer.FirstName : null,
                    t.ReferralCode
                })
                .ToListAsync();

            return Ok(new
            {
                total_tx = totalTx,
                total_tl = totalTl,
                total_usdt = totalUsdt,
                total_rub = totalRub,
                total_dealer_balance = totalDealerBalance,
                total_customers = totalCustomers,
                pending_tx = pendingTx,
                completed_tx = completedTxCount,
                recent_transactions = recentTx
            });
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> Transactions(int skip = 0, int take = 200)
        {
            await RequireAdmin();

            take = Math.Clamp(take, 1, 500);
            skip = Math.Max(skip, 0);

            var baseQuery = _db.TgTransactions.Include(t => t.Customer).OrderByDescending(t => t.CreatedAt);
            var totalCount = await _db.TgTransactions.CountAsync();

            var txs = await baseQuery
                .Skip(skip).Take(take)
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
                    t.AssignedOperatorId,
                    t.IsBuy,
                    t.CreatedAt,
                    t.CompletedAt,
                    t.CompletionCode,
                    CustomerName = t.Customer != null ? t.Customer.FirstName : null,
                    CustomerUsername = t.Customer != null ? t.Customer.Username : null
                })
                .ToListAsync();

            return Ok(new { transactions = txs, total_count = totalCount, has_more = skip + txs.Count < totalCount });
        }

        [HttpGet("dealers")]
        public async Task<IActionResult> GetDealers()
        {
            await RequireAdmin();

            var dealerUsers = await _db.Users
                .Where(u => u.DealerReferralCode != null)
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    name = (u.Firstname + " " + u.Lastname).Trim(),
                    dealer_code = u.DealerReferralCode,
                    is_active = u.Rank != Entity.Rank.Banned,
                    rank = (int)u.Rank,
                    created_at = u.CreatedDate
                })
                .ToListAsync();

            var tgDealers = await _db.TgDealers.ToListAsync();
            var txCounts = await _db.TgTransactions
                .Where(t => t.ReferralCode != null)
                .GroupBy(t => t.ReferralCode)
                .Select(g => new { Code = g.Key, Total = g.Count(), Completed = g.Count(t => t.Status == "completed") })
                .ToListAsync();

            // Kur bayatlama rozeti için: External bayilerde TgDealerRate, Branch bayilerde
            // o ofisin USDT/KRUB ExchangeRate'i baz alınır — en eski güncelleme tarihi döner.
            var dealerRateUpdates = await _db.TgDealerRates
                .GroupBy(r => r.DealerId)
                .Select(g => new { DealerId = g.Key, Oldest = g.Min(r => r.UpdatedAt) })
                .ToListAsync();

            var vaultOfficeMap = await _db.Vaults.Select(v => new { v.Id, v.OfficeId }).ToListAsync();
            var trackedCurrencyIds = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .Where(c => c.CurrencyCode == "USDT" || c.CurrencyCode == "KRUB")
                .Select(c => c.Id)
                .ToListAsync();
            var branchRateUpdates = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.ExchangeRate>()
                .Where(r => r.IsActive && trackedCurrencyIds.Contains(r.SourceCurrencyId))
                .GroupBy(r => r.OfficeId)
                .Select(g => new { OfficeId = g.Key, Oldest = g.Min(r => r.UpdatedAt) })
                .ToListAsync();

            var dealers = dealerUsers.Select(u =>
            {
                var tgd = tgDealers.FirstOrDefault(d => d.DealerCode == u.dealer_code);
                var txc = txCounts.FirstOrDefault(t => t.Code == u.dealer_code);

                DateTime? oldestRateUpdate = null;
                if (tgd != null)
                {
                    if (tgd.DealerType == TgDealerType.Branch && Guid.TryParse(tgd.VaultId, out var vaultGuid))
                    {
                        var officeId = vaultOfficeMap.FirstOrDefault(v => v.Id == vaultGuid)?.OfficeId;
                        if (officeId.HasValue)
                            oldestRateUpdate = branchRateUpdates.FirstOrDefault(b => b.OfficeId == officeId.Value)?.Oldest;
                    }
                    else
                    {
                        oldestRateUpdate = dealerRateUpdates.FirstOrDefault(r => r.DealerId == tgd.DealerId)?.Oldest;
                    }
                }

                return new
                {
                    u.id,
                    u.username,
                    u.name,
                    u.dealer_code,
                    dealer_name = tgd?.DealerName ?? u.name,
                    city = tgd?.City,
                    address = tgd?.Address,
                    u.is_active,
                    u.rank,
                    balance = tgd?.Balance ?? 0,
                    dealer_type = tgd?.DealerType.ToString() ?? "External",
                    vault_id = tgd?.VaultId,
                    commission_rate = tgd?.CommissionRate ?? 1.5m,
                    total_tx = txc?.Total ?? 0,
                    completed_tx = txc?.Completed ?? 0,
                    oldest_rate_update = oldestRateUpdate,
                    u.created_at
                };
            }).ToList();

            return Ok(new { dealers });
        }

        // DealerName'den okunaklı bir kod türetir (örn. "Ankara" -> "ANKARA01"), çakışırsa sayaç artırılır.
        private async Task<string> GenerateReadableDealerCodeAsync(string dealerName)
        {
            var baseCode = new string((dealerName ?? "").ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .Select(c => c switch
                {
                    'Ç' => 'C', 'Ğ' => 'G', 'İ' => 'I', 'Ö' => 'O', 'Ş' => 'S', 'Ü' => 'U',
                    _ => c
                })
                .ToArray());
            if (string.IsNullOrWhiteSpace(baseCode)) baseCode = "BAYI";

            for (var i = 1; i <= 99; i++)
            {
                var candidate = $"{baseCode}{i:00}";
                var taken = await _db.TgDealers.AnyAsync(d => d.DealerCode == candidate)
                    || await _db.Users.AnyAsync(u => u.DealerReferralCode == candidate);
                if (!taken) return candidate;
            }
            throw new ApiException(HttpStatusCode.Conflict, "Benzersiz bayi kodu üretilemedi, elle bir kod belirtin.");
        }

        [HttpPost("dealers")]
        public async Task<IActionResult> CreateDealer([FromBody] CreateDealerRequest req)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Name))
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı adı ve ad gerekli");

            if (req.DealerType == TgDealerType.Branch && !req.VaultId.HasValue)
                throw new ApiException(HttpStatusCode.BadRequest, "Şube tipi bayiler için Kasa (VaultId) seçilmelidir.");
            if (req.DealerType == TgDealerType.External && req.VaultId.HasValue)
                throw new ApiException(HttpStatusCode.BadRequest, "Harici bayilere Kasa bağlanamaz.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı bulunamadı. Önce sistem kullanıcısı oluşturun.");
            if (!string.IsNullOrEmpty(user.DealerReferralCode))
                throw new ApiException(HttpStatusCode.Conflict, "Bu kullanıcı zaten bir bayiye atanmış.");

            var dealerCode = string.IsNullOrWhiteSpace(req.DealerCode)
                ? await GenerateReadableDealerCodeAsync(req.Name)
                : req.DealerCode.Trim().ToUpperInvariant();

            if (await _db.TgDealers.AnyAsync(d => d.DealerCode == dealerCode))
                throw new ApiException(HttpStatusCode.Conflict, $"Bayi kodu '{dealerCode}' zaten kullanılıyor.");

            // Cari hesabın bağlanacağı Office: Şube ise kendi ofisi, Harici bayi ise Merkez.
            Guid partyOfficeId;
            string? vaultIdString = null;
            if (req.DealerType == TgDealerType.Branch)
            {
                var vault = await _db.Vaults.FirstOrDefaultAsync(v => v.Id == req.VaultId!.Value);
                if (vault == null)
                    throw new ApiException(HttpStatusCode.BadRequest, "Belirtilen Kasa bulunamadı.");
                partyOfficeId = vault.OfficeId;
                vaultIdString = vault.Id.ToString();
            }
            else
            {
                var merkez = await _db.Offices.FirstOrDefaultAsync(o => o.OfficeType == Entity.OfficeType.Merkez);
                if (merkez == null)
                    throw new ApiException(HttpStatusCode.InternalServerError, "Merkez ofis bulunamadı — harici bayi cari hesabı bağlanamıyor.");
                partyOfficeId = merkez.Id;
            }

            var tryCurrency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (tryCurrency == null)
                throw new ApiException(HttpStatusCode.InternalServerError, "TRY para birimi bulunamadı.");

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var party = new BaskentEnerji.Entity.Entities.ExchangeOffice.Party.Party
                {
                    OfficeId = partyOfficeId,
                    PartyCode = dealerCode,
                    Name = req.Name,
                    Type = BaskentEnerji.Entity.Entities.ExchangeOffice.Party.PartyType.Both,
                    IsActive = true
                };
                _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Party.Party>().Add(party);
                await _db.SaveChangesAsync();

                await _partyAccountService.CreateAccountAsync(party.Id, tryCurrency.Id);

                var dealer = new TgDealer
                {
                    DealerCode = dealerCode,
                    DealerName = req.Name,
                    DealerType = req.DealerType,
                    Balance = 0,
                    IsActive = true,
                    CommissionRate = req.CommissionRate ?? 1.5m,
                    PartyId = party.Id,
                    VaultId = vaultIdString,
                    CreatedAt = DateTime.UtcNow
                };
                _db.TgDealers.Add(dealer);

                user.DealerReferralCode = dealerCode;
                _db.Users.Update(user);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { success = true, dealer_code = dealerCode, party_id = party.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Sistem kullanicisi olusturma + bayi atamasini TEK ATOMIK cagriya indirger.
        // Onceden: /user/register (Rank=User=1 varsayilan) -> elle Rank yukselt -> CreateDealer,
        // 3 ayri, unutulmasi kolay adimdi. Burada NewUser cagrilir, Rank hemen Staff'a cekilir,
        // ardindan CreateDealer'in Party/PartyAccount/TgDealer olusturma govdesi ayni transaction'da tekrarlanir.
        [HttpPost("provision-dealer-user")]
        public async Task<IActionResult> ProvisionDealerUser(
            [FromBody] ProvisionDealerUserRequest req,
            [FromServices] IUserServiceCommand userService)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Mail)
                || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Name))
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı adı, mail, şifre ve ad gerekli.");
            if (req.DealerType == TgDealerType.Branch && !req.VaultId.HasValue)
                throw new ApiException(HttpStatusCode.BadRequest, "Şube tipi bayiler için Kasa (VaultId) seçilmelidir.");
            if (req.DealerType == TgDealerType.External && req.VaultId.HasValue)
                throw new ApiException(HttpStatusCode.BadRequest, "Harici bayilere Kasa bağlanamaz.");

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                Entity.Modals.ResponseModals.User.rsp_user_login created;
                try
                {
                    created = await userService.NewUser(new rm_user_register
                    {
                        Username = req.Username,
                        Mail = req.Mail,
                        Password = req.Password,
                        Firstname = req.Name
                    }, HttpContext);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ApiException(HttpStatusCode.Conflict, ex.Message);
                }

                var user = await _db.Users.FirstAsync(u => u.Id == created.UserInfo.Id);
                user.Rank = Entity.Rank.Staff;

                var dealerCode = string.IsNullOrWhiteSpace(req.DealerCode)
                    ? await GenerateReadableDealerCodeAsync(req.Name)
                    : req.DealerCode.Trim().ToUpperInvariant();

                if (await _db.TgDealers.AnyAsync(d => d.DealerCode == dealerCode))
                    throw new ApiException(HttpStatusCode.Conflict, $"Bayi kodu '{dealerCode}' zaten kullanılıyor.");

                Guid partyOfficeId;
                string? vaultIdString = null;
                if (req.DealerType == TgDealerType.Branch)
                {
                    var vault = await _db.Vaults.FirstOrDefaultAsync(v => v.Id == req.VaultId!.Value);
                    if (vault == null)
                        throw new ApiException(HttpStatusCode.BadRequest, "Belirtilen Kasa bulunamadı.");
                    partyOfficeId = vault.OfficeId;
                    vaultIdString = vault.Id.ToString();
                }
                else
                {
                    var merkez = await _db.Offices.FirstOrDefaultAsync(o => o.OfficeType == Entity.OfficeType.Merkez);
                    if (merkez == null)
                        throw new ApiException(HttpStatusCode.InternalServerError, "Merkez ofis bulunamadı — harici bayi cari hesabı bağlanamıyor.");
                    partyOfficeId = merkez.Id;
                }

                var tryCurrency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                    .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
                if (tryCurrency == null)
                    throw new ApiException(HttpStatusCode.InternalServerError, "TRY para birimi bulunamadı.");

                var party = new BaskentEnerji.Entity.Entities.ExchangeOffice.Party.Party
                {
                    OfficeId = partyOfficeId,
                    PartyCode = dealerCode,
                    Name = req.Name,
                    Type = BaskentEnerji.Entity.Entities.ExchangeOffice.Party.PartyType.Both,
                    IsActive = true
                };
                _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Party.Party>().Add(party);
                await _db.SaveChangesAsync();

                await _partyAccountService.CreateAccountAsync(party.Id, tryCurrency.Id);

                _db.TgDealers.Add(new TgDealer
                {
                    DealerCode = dealerCode,
                    DealerName = req.Name,
                    DealerType = req.DealerType,
                    Balance = 0,
                    IsActive = true,
                    CommissionRate = req.CommissionRate ?? 1.5m,
                    PartyId = party.Id,
                    VaultId = vaultIdString,
                    CreatedAt = DateTime.UtcNow
                });
                user.DealerReferralCode = dealerCode;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { success = true, user_id = user.Id, dealer_code = dealerCode });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Operatör icin ayni tek-adimli provizyon (bayiden daha basit: Party/PartyAccount gerekmez).
        [HttpPost("provision-operator-user")]
        public async Task<IActionResult> ProvisionOperatorUser(
            [FromBody] ProvisionOperatorUserRequest req,
            [FromServices] IUserServiceCommand userService)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Mail)
                || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Name))
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı adı, mail, şifre ve ad gerekli.");

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                Entity.Modals.ResponseModals.User.rsp_user_login created;
                try
                {
                    created = await userService.NewUser(new rm_user_register
                    {
                        Username = req.Username,
                        Mail = req.Mail,
                        Password = req.Password,
                        Firstname = req.Name
                    }, HttpContext);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ApiException(HttpStatusCode.Conflict, ex.Message);
                }

                var user = await _db.Users.FirstAsync(u => u.Id == created.UserInfo.Id);
                user.Rank = Entity.Rank.Staff;
                user.TelegramOperatorId = req.TelegramId;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { success = true, user_id = user.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        [HttpGet("operators")]
        public async Task<IActionResult> GetOperators()
        {
            await RequireAdmin();

            var operators = await _db.Users
                .Where(u => u.TelegramOperatorId != null)
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    name = u.Firstname + " " + u.Lastname,
                    telegram_id = u.TelegramOperatorId,
                    is_active = u.Rank != Entity.Rank.Banned,
                    created_at = u.CreatedDate
                })
                .ToListAsync();

            return Ok(new { operators });
        }

        [HttpPost("operators")]
        public async Task<IActionResult> CreateOperator([FromBody] CreateOperatorRequest req)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Username))
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı adı gerekli");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı bulunamadı.");
            if (user.TelegramOperatorId.HasValue)
                throw new ApiException(HttpStatusCode.Conflict, "Bu kullanıcı zaten bir operatöre atanmış.");

            user.TelegramOperatorId = req.TelegramId;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // ═══════════════════════════════════════════════
        // BOT OPERATÖRLERİ — TgOperators (Telegram /start ile kendi kendine kayıt)
        // ═══════════════════════════════════════════════

        [HttpGet("bot-operators")]
        public async Task<IActionResult> GetBotOperators()
        {
            await RequireAdmin();

            var botOperators = await _db.TgOperators
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var linkedIds = await _db.Users
                .Where(u => u.TelegramOperatorId != null)
                .Select(u => u.TelegramOperatorId!.Value)
                .ToListAsync();

            var result = botOperators.Select(o => new
            {
                operator_id = o.OperatorId,
                first_name = o.FirstName,
                username = o.Username,
                is_active = o.IsActive,
                is_admin = o.IsAdmin,
                created_at = o.CreatedAt,
                has_matching_system_user = linkedIds.Contains(o.OperatorId)
            });

            return Ok(new { bot_operators = result });
        }

        [HttpPost("bot-operators/{id}/activate")]
        public async Task<IActionResult> ActivateBotOperator(long id)
        {
            await RequireAdmin();
            var op = await _db.TgOperators.FindAsync(id);
            if (op == null)
                throw new ApiException(HttpStatusCode.NotFound, "Bot operatörü bulunamadı.");
            op.IsActive = true;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("bot-operators/{id}/deactivate")]
        public async Task<IActionResult> DeactivateBotOperator(long id)
        {
            await RequireAdmin();
            var op = await _db.TgOperators.FindAsync(id);
            if (op == null)
                throw new ApiException(HttpStatusCode.NotFound, "Bot operatörü bulunamadı.");
            op.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("operators/{userId}")]
        public async Task<IActionResult> DeleteOperator(Guid userId)
        {
            await RequireAdmin();

            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.TelegramOperatorId = null;
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpDelete("dealers/{userId}")]
        public async Task<IActionResult> DeleteDealer(Guid userId)
        {
            await RequireAdmin();

            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.DealerReferralCode = null;
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        public class rm_updatedealerinfo
        {
            public string? DealerName { get; set; }
            public string? City { get; set; }
            public string? Address { get; set; }
            public string? Firstname { get; set; }
            public string? Lastname { get; set; }
            public int? Rank { get; set; }
            public decimal? CommissionRate { get; set; }
        }

        // Bayi/Şube kartının kendi bilgilerini (görünen ad, şehir, adres) ve atanmış
        // kullanıcının ad/yetkisini TG Yönetim paneli içinden, ayrı sayfaya gitmeden düzenler.
        [HttpPut("dealers/{code}")]
        public async Task<IActionResult> UpdateDealerInfo(string code, [FromBody] rm_updatedealerinfo request)
        {
            await RequireAdmin();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.DealerReferralCode == code);
            if (user == null)
                throw new ApiException(HttpStatusCode.NotFound, "Bayi bulunamadı.");

            var tgd = await _db.TgDealers.FirstOrDefaultAsync(d => d.DealerCode == code);
            if (tgd != null)
            {
                if (request.DealerName != null) tgd.DealerName = request.DealerName;
                if (request.City != null) tgd.City = request.City;
                if (request.Address != null) tgd.Address = request.Address;
                if (request.CommissionRate.HasValue) tgd.CommissionRate = request.CommissionRate.Value;
            }

            if (request.Firstname != null) user.Firstname = request.Firstname;
            if (request.Lastname != null) user.Lastname = request.Lastname;
            if (request.Rank.HasValue)
            {
                if (!Enum.IsDefined(typeof(Entity.Rank), request.Rank.Value))
                    throw new ApiException(HttpStatusCode.BadRequest, "Geçersiz rütbe değeri.");
                var requestedRank = (Entity.Rank)request.Rank.Value;
                if (requestedRank == Entity.Rank.Owner && !await _validationService.IsOwnerAsync())
                    throw new ApiException(HttpStatusCode.Forbidden, "Owner rütbesi yalnızca bir Owner tarafından verilebilir.");
                user.Rank = requestedRank;
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ═══════════════════════════════════════════════
        // BAYİ KUR YÖNETİMİ — bayiye özel alış/satış (settlement) kuru.
        // Müşteriye gösterilen kur (TgExchangeRate) ile KASITLI olarak ayrı: kâr, aradaki
        // farktan (spread) doğar, sabit bir komisyon yüzdesinden değil.
        // ═══════════════════════════════════════════════

        [HttpGet("dealers/{code}/rates")]
        public async Task<IActionResult> GetDealerRates(string code)
        {
            await RequireAdmin();

            var dealer = await _db.TgDealers.FirstOrDefaultAsync(d => d.DealerCode == code);
            if (dealer == null)
                throw new ApiException(HttpStatusCode.NotFound, "Bayi bulunamadı.");

            var rates = await _db.TgDealerRates
                .Where(r => r.DealerId == dealer.DealerId)
                .Select(r => new { r.Currency, r.BuyRate, r.SellRate, r.UpdatedAt })
                .ToListAsync();

            return Ok(new { dealer_code = code, rates });
        }

        public class UpsertDealerRateRequest
        {
            public string Currency { get; set; } = "";
            public decimal BuyRate { get; set; }
            public decimal SellRate { get; set; }
        }

        [HttpPost("dealers/{code}/rates")]
        public async Task<IActionResult> UpsertDealerRate(string code, [FromBody] UpsertDealerRateRequest req)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Currency))
                throw new ApiException(HttpStatusCode.BadRequest, "Para birimi gerekli.");
            if (req.BuyRate <= 0 || req.SellRate <= 0)
                throw new ApiException(HttpStatusCode.BadRequest, "Kurlar sıfır veya negatif olamaz.");
            if (req.BuyRate >= req.SellRate)
                throw new ApiException(HttpStatusCode.BadRequest, "Alış kuru satış kurundan küçük olmalıdır.");

            var dealer = await _db.TgDealers.FirstOrDefaultAsync(d => d.DealerCode == code);
            if (dealer == null)
                throw new ApiException(HttpStatusCode.NotFound, "Bayi bulunamadı.");

            var currencyCode = TgCurrencyMapper.ToSystemCurrencyCode(req.Currency);

            var rate = await _db.TgDealerRates
                .FirstOrDefaultAsync(r => r.DealerId == dealer.DealerId && r.Currency == currencyCode);
            if (rate == null)
            {
                rate = new TgDealerRate { DealerId = dealer.DealerId, Currency = currencyCode };
                _db.TgDealerRates.Add(rate);
            }
            else
            {
                // Geçmişe eski değeri kaydet (yeni kayıt ilk kez giriliyorsa geçmiş yok)
                _db.TgDealerRateHistories.Add(new TgDealerRateHistory
                {
                    DealerId = dealer.DealerId,
                    Currency = currencyCode,
                    OldBuyRate = rate.BuyRate,
                    OldSellRate = rate.SellRate,
                    NewBuyRate = req.BuyRate,
                    NewSellRate = req.SellRate,
                    ChangedAt = DateTime.UtcNow
                });
            }
            rate.BuyRate = req.BuyRate;
            rate.SellRate = req.SellRate;
            rate.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, currency = currencyCode, buyRate = rate.BuyRate, sellRate = rate.SellRate, updatedAt = rate.UpdatedAt });
        }

        [HttpGet("dealers/{code}/rates/{currency}/history")]
        public async Task<IActionResult> GetDealerRateHistory(string code, string currency, [FromQuery] int limit = 10)
        {
            await RequireAdmin();

            var dealer = await _db.TgDealers.FirstOrDefaultAsync(d => d.DealerCode == code);
            if (dealer == null)
                throw new ApiException(HttpStatusCode.NotFound, "Bayi bulunamadı.");

            var currencyCode = TgCurrencyMapper.ToSystemCurrencyCode(currency);

            var history = await _db.TgDealerRateHistories
                .Where(h => h.DealerId == dealer.DealerId && h.Currency == currencyCode)
                .OrderByDescending(h => h.ChangedAt)
                .Take(limit)
                .Select(h => new { h.OldBuyRate, h.OldSellRate, h.NewBuyRate, h.NewSellRate, h.ChangedAt })
                .ToListAsync();

            return Ok(new { history });
        }

        public class BulkUpdateDealerRateRequest
        {
            public string Currency { get; set; } = "";
            public decimal BuyRate { get; set; }
            public decimal SellRate { get; set; }
        }

        [HttpPost("dealers/rates/bulk")]
        public async Task<IActionResult> BulkUpdateDealerRate([FromBody] BulkUpdateDealerRateRequest req)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Currency))
                throw new ApiException(HttpStatusCode.BadRequest, "Para birimi gerekli.");
            if (req.BuyRate <= 0 || req.SellRate <= 0)
                throw new ApiException(HttpStatusCode.BadRequest, "Kurlar sıfır veya negatif olamaz.");
            if (req.BuyRate >= req.SellRate)
                throw new ApiException(HttpStatusCode.BadRequest, "Alış kuru satış kurundan küçük olmalıdır.");

            var currencyCode = TgCurrencyMapper.ToSystemCurrencyCode(req.Currency);

            // Sadece Harici (External) bayiler — Şube tipi kendi ofis kurunu kullanır.
            var externalDealers = await _db.TgDealers
                .Where(d => d.IsActive && d.DealerType == TgDealerType.External)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var affected = 0;
            foreach (var dealer in externalDealers)
            {
                var rate = await _db.TgDealerRates
                    .FirstOrDefaultAsync(r => r.DealerId == dealer.DealerId && r.Currency == currencyCode);
                if (rate == null)
                {
                    rate = new TgDealerRate { DealerId = dealer.DealerId, Currency = currencyCode };
                    _db.TgDealerRates.Add(rate);
                }
                else
                {
                    _db.TgDealerRateHistories.Add(new TgDealerRateHistory
                    {
                        DealerId = dealer.DealerId,
                        Currency = currencyCode,
                        OldBuyRate = rate.BuyRate,
                        OldSellRate = rate.SellRate,
                        NewBuyRate = req.BuyRate,
                        NewSellRate = req.SellRate,
                        ChangedAt = now
                    });
                }
                rate.BuyRate = req.BuyRate;
                rate.SellRate = req.SellRate;
                rate.UpdatedAt = now;
                affected++;
            }

            await _db.SaveChangesAsync();

            return Ok(new { success = true, currency = currencyCode, dealersUpdated = affected });
        }

        // ═══════════════════════════════════════════════
        // ŞUBE (BRANCH) BAYİ KURU — o ofisin gerçek ExchangeRates'i (Kasa/WAC muhasebesini
        // besleyen kur). Mevcut ExchangeController./rates altyapısını sarmalar; TG Yönetim →
        // Bayiler ekranından Manuel Kur Yönetimi'ne gitmeden Şube kuru düzenlenebilsin diye.
        // ═══════════════════════════════════════════════

        private async Task<(Guid officeId, Guid tryCurrencyId)> ResolveBranchContextAsync(string dealerCode)
        {
            var dealer = await _db.TgDealers.FirstOrDefaultAsync(d => d.DealerCode == dealerCode);
            if (dealer == null || dealer.DealerType != TgDealerType.Branch || !Guid.TryParse(dealer.VaultId, out var vaultId))
                throw new ApiException(HttpStatusCode.BadRequest, "Bu bayi Şube tipinde değil veya Kasa'sı yok.");

            var vault = await _db.Vaults.FirstOrDefaultAsync(v => v.Id == vaultId);
            if (vault == null)
                throw new ApiException(HttpStatusCode.NotFound, "Kasa bulunamadı.");

            var tryCurrency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (tryCurrency == null)
                throw new ApiException(HttpStatusCode.InternalServerError, "TRY para birimi bulunamadı.");

            return (vault.OfficeId, tryCurrency.Id);
        }

        [HttpGet("dealers/{code}/branch-rates")]
        public async Task<IActionResult> GetBranchRates(string code)
        {
            await RequireAdmin();
            var (officeId, tryCurrencyId) = await ResolveBranchContextAsync(code);

            var results = new System.Collections.Generic.List<object>();
            foreach (var cur in new[] { "USDT", "KRUB" })
            {
                var currency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                    .FirstOrDefaultAsync(c => c.CurrencyCode == cur);
                if (currency == null) continue;

                var rate = await _exchangeRateService.GetCurrentRateAsync(officeId, currency.Id, tryCurrencyId);
                results.Add(new
                {
                    currency = cur,
                    buyRate = rate?.BuyRate ?? 0,
                    sellRate = rate?.SellRate ?? 0,
                    updatedAt = rate?.UpdatedAt,
                    sourceCurrencyId = currency.Id,
                    targetCurrencyId = tryCurrencyId
                });
            }

            return Ok(new { dealer_code = code, rates = results });
        }

        public class UpsertBranchRateRequest
        {
            public string Currency { get; set; } = "";
            public decimal BuyRate { get; set; }
            public decimal SellRate { get; set; }
        }

        [HttpPost("dealers/{code}/branch-rates")]
        public async Task<IActionResult> UpsertBranchRate(string code, [FromBody] UpsertBranchRateRequest req)
        {
            await RequireAdmin();

            if (req.BuyRate <= 0 || req.SellRate <= 0)
                throw new ApiException(HttpStatusCode.BadRequest, "Kurlar sıfır veya negatif olamaz.");
            if (req.BuyRate >= req.SellRate)
                throw new ApiException(HttpStatusCode.BadRequest, "Alış kuru satış kurundan küçük olmalıdır.");

            var (officeId, tryCurrencyId) = await ResolveBranchContextAsync(code);
            var currencyCode = TgCurrencyMapper.ToSystemCurrencyCode(req.Currency);
            var currency = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .FirstOrDefaultAsync(c => c.CurrencyCode == currencyCode);
            if (currency == null)
                throw new ApiException(HttpStatusCode.BadRequest, $"Para birimi bulunamadı: {currencyCode}");

            var updated = await _exchangeRateService.CreateOrUpdateRateAsync(officeId, currency.Id, tryCurrencyId, req.BuyRate, req.SellRate);

            return Ok(new { success = true, currency = currencyCode, buyRate = updated.BuyRate, sellRate = updated.SellRate, updatedAt = updated.UpdatedAt });
        }

        [HttpGet("dealers/{code}/branch-rates/{currency}/history")]
        public async Task<IActionResult> GetBranchRateHistory(string code, string currency, [FromQuery] int limit = 10)
        {
            await RequireAdmin();
            var (officeId, tryCurrencyId) = await ResolveBranchContextAsync(code);
            var currencyCode = TgCurrencyMapper.ToSystemCurrencyCode(currency);
            var cur = await _db.Set<BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency>()
                .FirstOrDefaultAsync(c => c.CurrencyCode == currencyCode);
            if (cur == null)
                throw new ApiException(HttpStatusCode.BadRequest, $"Para birimi bulunamadı: {currencyCode}");

            var rates = await _exchangeRateService.GetRateHistoryAsync(officeId, cur.Id, tryCurrencyId, limit);
            var history = rates.Select((r, i) => new
            {
                changedAt = r.UpdatedAt,
                newBuyRate = r.BuyRate,
                newSellRate = r.SellRate,
                oldBuyRate = i + 1 < rates.Count ? rates[i + 1].BuyRate : (decimal?)null,
                oldSellRate = i + 1 < rates.Count ? rates[i + 1].SellRate : (decimal?)null,
            });
            return Ok(new { history });
        }

        [HttpGet("crypto-deposits")]
        public async Task<IActionResult> CryptoDeposits()
        {
            await RequireAdmin();

            var deposits = await _db.TgCryptoDeposits
                .OrderByDescending(d => d.DepositTime)
                .Select(d => new
                {
                    Id = d.DepositId,
                    d.TransactionId,
                    d.DealerId,
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

        [HttpGet("bot-status")]
        public async Task<IActionResult> BotStatus()
        {
            await RequireAdmin();

            var heartbeats = await _db.TgBotHeartbeats.ToListAsync();
            var result = heartbeats.ToDictionary(
                h => h.BotName,
                h =>
                {
                    var ageSeconds = h.LastHeartbeat.HasValue
                        ? (DateTime.Now - h.LastHeartbeat.Value).TotalSeconds
                        : (double?)null;
                    return new
                    {
                        last_heartbeat = h.LastHeartbeat,
                        last_transaction_at = h.LastTransactionAt,
                        active_sessions = h.ActiveSessions ?? 0,
                        status = ageSeconds.HasValue && ageSeconds < 120 ? "online" : "offline",
                        age_seconds = ageSeconds.HasValue ? (int)ageSeconds : (int?)null
                    };
                });

            var expected = new[] { "main_bot", "operator_bot", "ruble_bot" };
            foreach (var name in expected)
            {
                if (!result.ContainsKey(name))
                    result[name] = new { last_heartbeat = (DateTime?)null, last_transaction_at = (DateTime?)null, active_sessions = 0, status = "unknown", age_seconds = (int?)null };
            }

            return Ok(result);
        }

        [HttpGet("baskent-queue")]
        public async Task<IActionResult> BaskentQueue()
        {
            await RequireAdmin();

            var items = await _db.TgApiQueue
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            var pending = items.Count(i => i.Status == "pending");
            var failed = items.Count(i => i.Status == "failed");
            var success = items.Count(i => i.Status == "success");

            return Ok(new { items, summary = new { pending, failed, success } });
        }

        [HttpPost("baskent-queue/{queueId}/retry")]
        public async Task<IActionResult> RetryQueue(int queueId)
        {
            await RequireAdmin();

            var item = await _db.TgApiQueue.FindAsync(queueId);
            if (item != null)
            {
                item.Status = "pending";
                item.Attempts = 0;
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpGet("login-logs")]
        public async Task<IActionResult> LoginLogs()
        {
            await RequireAdmin();

            var logs = await _db.TgLoginLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();

            return Ok(new { logs });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            await RequireAdmin();

            var today = DateTime.Today;
            var weekAgo = DateTime.Now.AddDays(-7);
            var monthAgo = DateTime.Now.AddDays(-30);

            var todayCount = await _db.TgTransactions.CountAsync(t => t.CreatedAt >= today);
            var weekCount = await _db.TgTransactions.CountAsync(t => t.CreatedAt >= weekAgo);
            var monthCount = await _db.TgTransactions.CountAsync(t => t.CreatedAt >= monthAgo);
            var activeDealers = await _db.TgTransactions
                .Where(t => t.ReferralCode != null)
                .Select(t => t.ReferralCode)
                .Distinct()
                .CountAsync();

            var recentLogins = await _db.TgLoginLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                today_count = todayCount,
                week_count = weekCount,
                month_count = monthCount,
                active_dealers = activeDealers,
                recentLogins
            });
        }
        [HttpGet("exchange-rates")]
        public async Task<IActionResult> ExchangeRates()
        {
            await RequireAdmin();
            var rates = await _db.TgExchangeRates.ToListAsync();
            return Ok(new { rates });
        }

        [HttpPut("exchange-rates/{id}")]
        public async Task<IActionResult> UpdateExchangeRate(int id, [FromBody] UpdateRateRequest req)
        {
            await RequireAdmin();

            var rate = await _db.TgExchangeRates.FindAsync(id);
            if (rate == null)
                throw new ApiException(HttpStatusCode.NotFound, "Kur bulunamadı");

            rate.BuyRate = req.BuyRate;
            rate.SellRate = req.SellRate;
            rate.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, rate });
        }

        [HttpPost("exchange-rates")]
        public async Task<IActionResult> CreateExchangeRate([FromBody] CreateRateRequest req)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Currency))
                throw new ApiException(HttpStatusCode.BadRequest, "Para birimi gerekli");

            var existing = await _db.TgExchangeRates.FirstOrDefaultAsync(r => r.Currency == req.Currency.ToUpper());
            if (existing != null)
                throw new ApiException(HttpStatusCode.Conflict, "Bu para birimi zaten mevcut");

            var rate = new Entity.Entities.Telegram.TgExchangeRate
            {
                Currency = req.Currency.ToUpper(),
                BuyRate = req.BuyRate,
                SellRate = req.SellRate,
                UpdatedAt = DateTime.Now
            };
            _db.TgExchangeRates.Add(rate);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, rate });
        }

        [HttpGet("crypto-summary")]
        public async Task<IActionResult> CryptoSummary()
        {
            await RequireAdmin();

            var deposits = await _db.TgCryptoDeposits.ToListAsync();
            var transactions = await _db.TgTransactions
                .Where(t => t.Currency == "USDT")
                .ToListAsync();

            var totalDeposits = deposits.Sum(d => d.Amount);
            var pendingDeposits = deposits.Where(d => d.Status == "pending").Sum(d => d.Amount);
            var confirmedDeposits = deposits.Where(d => d.Status == "confirmed" || d.Status == "completed").Sum(d => d.Amount);
            var pendingCount = deposits.Count(d => d.Status == "pending");
            var confirmedCount = deposits.Count(d => d.Status == "confirmed" || d.Status == "completed");

            var verifiedTx = transactions.Count(t => t.CryptoVerified == true);
            var unverifiedTx = transactions.Count(t => t.Txid != null && t.CryptoVerified != true);

            return Ok(new
            {
                total_deposits = totalDeposits,
                pending_deposits = pendingDeposits,
                confirmed_deposits = confirmedDeposits,
                pending_count = pendingCount,
                confirmed_count = confirmedCount,
                verified_transactions = verifiedTx,
                unverified_transactions = unverifiedTx,
                total_usdt_transactions = transactions.Count
            });
        }
    }

    public class UpdateRateRequest
    {
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
    }

    public class CreateRateRequest
    {
        public string Currency { get; set; } = "";
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
    }

    public class CreateDealerRequest
    {
        public string Username { get; set; } = "";
        public string Name { get; set; } = "";
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TgDealerType DealerType { get; set; } = TgDealerType.External;
        public Guid? VaultId { get; set; }
        public decimal? CommissionRate { get; set; }
        public string? DealerCode { get; set; }
    }

    public class CreateOperatorRequest
    {
        public string Username { get; set; } = "";
        public long? TelegramId { get; set; }
    }

    public class ProvisionDealerUserRequest
    {
        public string Username { get; set; } = "";
        public string Mail { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TgDealerType DealerType { get; set; } = TgDealerType.External;
        public Guid? VaultId { get; set; }
        public decimal? CommissionRate { get; set; }
        public string? DealerCode { get; set; }
    }

    public class ProvisionOperatorUserRequest
    {
        public string Username { get; set; } = "";
        public string Mail { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";
        public long? TelegramId { get; set; }
    }
}
