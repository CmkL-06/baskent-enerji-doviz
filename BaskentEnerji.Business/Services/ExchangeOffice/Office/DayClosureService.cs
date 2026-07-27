using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class DayClosureService : IDayClosureService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IWacService _wacService;
        private readonly ValidationService _validationService;

        private static readonly TimeZoneInfo TurkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        public DayClosureService(BaskentEnerjiDbContext context, IWacService wacService, ValidationService validationService)
        {
            _context = context;
            _wacService = wacService;
            _validationService = validationService;
        }

        // Sertleştirme: GetDayStatusAsync ve CanTransactAsync, "son kapanış" + "onay bekleyen kapanış"
        // sorgularını ve buna bağlı "beklenen sonraki iş günü" mantığını neredeyse birebir kopyalamıştı
        // — biri güncellenip diğeri unutulursa sessiz tutarsızlık riski taşıyordu. Artık tek yerden okunuyor.
        private async Task<(DayClosure? lastClosure, DayClosure? pendingApproval)> GetLastClosureStateAsync(Guid officeId)
        {
            var lastClosure = await _context.DayClosures
                .Include(d => d.ClosedByUser)
                .Where(d => d.OfficeId == officeId && (d.Status == DayClosureStatus.Closed || d.Status == DayClosureStatus.AutoClosed))
                .OrderByDescending(d => d.BusinessDate)
                .FirstOrDefaultAsync();

            var pendingApproval = await _context.DayClosures
                .Where(d => d.OfficeId == officeId && d.Status == DayClosureStatus.PendingApproval)
                .OrderByDescending(d => d.BusinessDate)
                .FirstOrDefaultAsync();

            return (lastClosure, pendingApproval);
        }

        public async Task<vm_daystatus> GetDayStatusAsync(Guid officeId)
        {
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz).Date;

            var (lastClosure, pendingApproval) = await GetLastClosureStateAsync(officeId);

            var status = new vm_daystatus
            {
                OfficeId = officeId,
                SystemBalances = new List<vm_dayclosurebalance>()
            };

            if (lastClosure == null)
            {
                status.IsDayOpen = true;
                status.CanTransact = true;
                status.CurrentBusinessDate = today;
                status.HasUnclosedDays = false;
                status.UnclosedDayCount = 0;
                await PopulateSystemBalances(status, officeId);
                await PopulateActivitySummaryAsync(status, officeId, today, lastClosure);
                return status;
            }

            var lastClosedDate = lastClosure.BusinessDate.Date;
            var yesterday = today.AddDays(-1);

            if (lastClosedDate >= yesterday)
            {
                status.IsDayOpen = true;
                status.CanTransact = true;
                status.CurrentBusinessDate = today;
                status.LastClosedDate = lastClosedDate;
                status.HasUnclosedDays = false;
                status.UnclosedDayCount = 0;
            }
            else
            {
                var firstUnclosed = lastClosedDate.AddDays(1);
                var unclosedDays = (int)(today - firstUnclosed).TotalDays + 1;

                status.IsDayOpen = false;
                status.CanTransact = false;
                status.CurrentBusinessDate = today;
                status.LastClosedDate = lastClosedDate;
                status.HasUnclosedDays = true;
                status.UnclosedDayCount = unclosedDays;
                status.FirstUnclosedDate = firstUnclosed;
                status.BlockReason = (pendingApproval != null && pendingApproval.BusinessDate.Date == firstUnclosed)
                    ? $"{firstUnclosed:dd.MM.yyyy} tarihli kapanış, sayım farkı 500 TL eşiğini aştığı için Owner onayı bekliyor."
                    : $"{firstUnclosed:dd.MM.yyyy} tarihli gün kapanışı yapılmadı. İşlem yapabilmek için önce kapanış gerekli.";
            }

            await PopulateSystemBalances(status, officeId);
            await PopulateActivitySummaryAsync(status, officeId, today, lastClosure);
            return status;
        }

        public async Task<bool> CanTransactAsync(Guid officeId)
        {
            // PendingApproval bir gerçek kapanış sayılmaz — ama bloklamak için lastClosure'a bağımlı
            // kalınırsa, hiç gerçek kapanış yokken (yeni ofis/vault) ilk günün PendingApproval kapanışı
            // hiç engel oluşturmaz (lastClosure null olduğu için kontrol hep true dönerdi).
            var (lastClosure, pendingApproval) = await GetLastClosureStateAsync(officeId);

            if (pendingApproval != null)
            {
                var expectedDate = lastClosure != null ? lastClosure.BusinessDate.Date.AddDays(1) : pendingApproval.BusinessDate.Date;
                if (pendingApproval.BusinessDate.Date == expectedDate)
                    return false;
            }

            if (lastClosure == null)
                return true;

            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz).Date;
            var yesterday = today.AddDays(-1);

            return lastClosure.BusinessDate.Date >= yesterday;
        }

        public async Task<vm_dayclosure> CloseDayAsync(rm_dayclosure request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // (OfficeId, BusinessDate) üzerinde bir unique constraint yok — iki kullanıcı aynı
                // ofis için aynı anda "gün kapat" çağırırsa, ikisi de aynı lastClosure/pendingForDate
                // durumunu okuyup ikisi de kontrolü geçebilir ve aynı gün için iki DayClosure kaydı
                // (çift WAC/bakiye ayarlaması) oluşabilirdi. sp_getapplock ile aynı ofis için kapanış
                // işlemleri serileştiriliyor — kilit transaction ile birlikte otomatik serbest kalır.
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000",
                    $"DayClosure_{request.OfficeId}");

                var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz).Date;
                var businessDate = request.BusinessDate.Date;

                // PendingApproval henüz "gerçekten kapanmış" sayılmaz — Owner onaylayana kadar
                // bir sonraki günün işlemleri bloklanmaya devam etmeli.
                var lastClosure = await _context.DayClosures
                    .Where(d => d.OfficeId == request.OfficeId && (d.Status == DayClosureStatus.Closed || d.Status == DayClosureStatus.AutoClosed))
                    .OrderByDescending(d => d.BusinessDate)
                    .FirstOrDefaultAsync();

                var expectedDate = lastClosure != null ? lastClosure.BusinessDate.Date.AddDays(1) : businessDate;

                if (lastClosure != null && businessDate < expectedDate)
                    throw new ApiException(HttpStatusCode.BadRequest, "Bu tarih zaten kapatılmış.");

                if (businessDate > today)
                    throw new ApiException(HttpStatusCode.BadRequest, "Gelecek tarih kapatılamaz.");

                var pendingForDate = await _context.DayClosures.FirstOrDefaultAsync(d =>
                    d.OfficeId == request.OfficeId && d.BusinessDate.Date == businessDate && d.Status == DayClosureStatus.PendingApproval);
                if (pendingForDate != null)
                    throw new ApiException(HttpStatusCode.BadRequest, "Bu gün için kapanış Owner onayı bekliyor, tekrar kapatılamaz.");

                var vault = await _context.Vaults
                    .Include(v => v.Balances).ThenInclude(b => b.Currency)
                    .FirstOrDefaultAsync(v => v.OfficeId == request.OfficeId && v.IsActive && v.Type == Vault.VaultType.Main);

                if (vault == null)
                    throw new ApiException(HttpStatusCode.NotFound, "Aktif ana kasa bulunamadı.");

                var userId = Guid.Parse(_validationService.GetUserID());

                // Auto-close intermediate days if there's a gap
                if (lastClosure != null && businessDate > expectedDate)
                {
                    for (var d = expectedDate; d < businessDate; d = d.AddDays(1))
                    {
                        await AutoCloseDayAsync(request.OfficeId, vault.Id, d, userId);
                    }
                }

                // Get transaction stats for the day — businessDate Türkiye yerel takvim günüdür,
                // TransactionDate ise UTC olarak kaydediliyor (bkz. ZReportService.GetDailyZReport'taki
                // aynı düzeltme notu) — sınırlar UTC'ye çevrilmeden karşılaştırılırsa TRT 00:00-03:00
                // arası işlemler yanlış güne düşer ve kapanışın kâr/işlem sayısı hatalı hesaplanır.
                var dayStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(businessDate, DateTimeKind.Unspecified), TurkeyTz);
                var dayEnd = dayStart.AddDays(1);
                var dayTransactions = await _context.Transactions
                    .Where(t => t.VaultId == vault.Id
                        && t.TransactionDate >= dayStart && t.TransactionDate < dayEnd
                        && t.Status == TransactionStatus.Completed && !t.IsDeleted)
                    .ToListAsync();

                var totalProfit = dayTransactions.Sum(t => t.Profit);
                var txCount = dayTransactions.Count;

                // Build closure
                var closure = new DayClosure
                {
                    OfficeId = request.OfficeId,
                    VaultId = vault.Id,
                    BusinessDate = businessDate,
                    ClosedByUserId = userId,
                    ClosedAt = DateTime.UtcNow,
                    Status = DayClosureStatus.Closed,
                    TotalRealizedProfit = totalProfit,
                    TransactionCount = txCount,
                    Notes = request.Notes ?? "",
                    IsAutoGenerated = false,
                    RejectionNote = "",
                    Details = new List<DayClosureDetail>()
                };

                bool hasAnyDiscrepancy = false;
                decimal totalDiscrepancyTRY = 0;

                // Gün kapanışında girilen fiziksel sayım, ayrı "Kasa Sayımı" (VaultCount) mekanizmasıyla
                // birleştirilir — DayClosure.VaultCountId FK'ı zaten buna işaret ediyordu ama hiç
                // doldurulmuyordu, bu da gün kapanışı yapılsa bile "Kasa Sayımı Gerekli" uyarısının
                // ayrıca çıkmasına yol açıyordu. Artık gün kapanışı = kasa sayımı, tek bir eylem.
                var vaultCount = new VaultCount
                {
                    VaultId = vault.Id,
                    OfficeId = request.OfficeId,
                    UserId = userId,
                    CountDate = DateTime.UtcNow,
                    HasDiscrepancy = false,
                    DiscrepancyDetails = "",
                    IsSystemGenerated = false,
                    CountDetails = new List<VaultCountDetail>()
                };

                // İlk geçiş: sadece farkları hesapla ve TL karşılığı toplam sapmayı bul.
                // Bakiye/WAC mutasyonu burada YAPILMAZ — eşik aşılırsa bunlar Owner onayına kadar ertelenir.
                foreach (var detail in request.Details)
                {
                    var balance = vault.Balances.FirstOrDefault(b => b.CurrencyId == detail.CurrencyId);
                    var systemBalance = balance?.Balance ?? 0;
                    var discrepancy = detail.PhysicalCount - systemBalance;

                    if (Math.Abs(discrepancy) > FinancialConstants.DiscrepancyThreshold && string.IsNullOrWhiteSpace(detail.DiscrepancyNote))
                        throw new ApiException(HttpStatusCode.BadRequest, $"Sayım farkı olan döviz için açıklama zorunludur. ({balance?.Currency?.CurrencyCode ?? detail.CurrencyId.ToString()})");

                    vaultCount.CountDetails.Add(new VaultCountDetail
                    {
                        CurrencyId = detail.CurrencyId,
                        ActualAmount = detail.PhysicalCount,
                        SystemAmount = systemBalance,
                        Discrepancy = discrepancy
                    });
                    if (Math.Abs(discrepancy) > FinancialConstants.DiscrepancyThreshold) vaultCount.HasDiscrepancy = true;

                    var wac = await _wacService.GetWacAsync(vault.Id, detail.CurrencyId);

                    closure.Details.Add(new DayClosureDetail
                    {
                        DayClosureId = closure.Id,
                        CurrencyId = detail.CurrencyId,
                        SystemBalance = systemBalance,
                        PhysicalCount = detail.PhysicalCount,
                        Discrepancy = discrepancy,
                        DiscrepancyNote = detail.DiscrepancyNote,
                        WacAtClose = wac,
                        OpeningBalance = detail.PhysicalCount,
                        OpeningWac = wac
                    });

                    if (Math.Abs(discrepancy) > FinancialConstants.DiscrepancyThreshold)
                    {
                        hasAnyDiscrepancy = true;

                        var rateToTRY = balance?.Currency?.CurrencyCode == "TRY"
                            ? 1m
                            : (wac > 0 ? wac : await GetRateToTRYAsync(detail.CurrencyId));
                        totalDiscrepancyTRY += Math.Abs(discrepancy) * rateToTRY;
                    }
                }

                const decimal ApprovalThresholdTRY = 500m;
                bool requiresApproval = totalDiscrepancyTRY > ApprovalThresholdTRY;
                closure.Status = requiresApproval ? DayClosureStatus.PendingApproval : DayClosureStatus.Closed;

                // İkinci geçiş: eşik aşılmadıysa (ya da hiç fark yoksa) bakiye/WAC düzeltmelerini hemen uygula.
                // Aşıldıysa hiçbir şey uygulanmaz — Owner onayladığında ApproveDayClosureAsync bu satırları
                // (closure.Details üzerinden) aynı şekilde uygular.
                if (!requiresApproval)
                {
                    foreach (var d in closure.Details.Where(d => Math.Abs(d.Discrepancy) > FinancialConstants.DiscrepancyThreshold))
                    {
                        var balance = vault.Balances.FirstOrDefault(b => b.CurrencyId == d.CurrencyId);
                        if (balance != null)
                        {
                            balance.Balance = d.PhysicalCount;
                            balance.LastUpdated = DateTime.UtcNow;
                        }
                        else
                        {
                            _context.VaultBalances.Add(new VaultBalance
                            {
                                VaultId = vault.Id,
                                CurrencyId = d.CurrencyId,
                                Balance = d.PhysicalCount,
                                LastUpdated = DateTime.UtcNow
                            });
                        }

                        await _wacService.AdjustWacQuantityAsync(vault.Id, d.CurrencyId, d.PhysicalCount, WacAdjustReason.DayClosure);

                        _context.VaultBalanceHistories.Add(new VaultBalanceHistory
                        {
                            VaultId = vault.Id,
                            CurrencyId = d.CurrencyId,
                            Balance = d.Discrepancy,
                            Description = $"Gün kapanışı sayım farkı: {d.Discrepancy:+0.####;-0.####} ({businessDate:dd.MM.yyyy})",
                            UserId = userId,
                            TransactionType = TransactionType.Adjustment,
                            IsDeleted = false,
                            IsGhost = false,
                            IsParty = false
                        });
                    }
                }

                _context.VaultCounts.Add(vaultCount);
                await _context.SaveChangesAsync();

                closure.VaultCountId = vaultCount.Id;
                // ShouldCount/LastCountDate yalnızca kapanış gerçekten tamamlandıysa (onay beklemiyorsa) sıfırlanır —
                // aksi halde onay bekleyen bir kapanış "sayım yapıldı" izlenimi verip asıl kontrolü atlatabilir.
                if (!requiresApproval)
                {
                    vault.ShouldCount = false;
                    vault.LastCountDate = DateTime.UtcNow;
                }

                _context.DayClosures.Add(closure);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToViewModel(closure, hasAnyDiscrepancy);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<decimal> GetRateToTRYAsync(Guid currencyId)
        {
            var tryCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (tryCurrency == null || currencyId == tryCurrency.Id)
                return 1m;

            var rate = await _context.ExchangeRates
                .Where(r => r.SourceCurrencyId == currencyId && r.TargetCurrencyId == tryCurrency.Id && r.IsActive)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            // Sertleştirme: bu metod sadece WAC=0 olan (hiç alışı yapılmamış) bir para biriminde
            // sayım farkı çıktığında çağrılır. Kur da bulunamazsa eskiden 0 dönülüyordu — bu da
            // o para biriminin sayım farkını "0 TL" sayıp 500 TL onay eşiğini sessizce atlatabiliyordu.
            // Artık böyle bir durumda gün kapanışı, kur tanımlanana kadar engelleniyor.
            if (rate == null)
            {
                var currency = await _context.Currencies.FindAsync(currencyId);
                throw new ApiException(HttpStatusCode.BadRequest,
                    $"'{currency?.CurrencyCode ?? currencyId.ToString()}' için WAC veya TRY kuru bulunamadığından sayım farkı TL karşılığı hesaplanamıyor. Gün kapanışından önce bu para birimi için bir kur tanımlayın.");
            }

            return rate.BuyRate;
        }

        public async Task<List<vm_dayclosure>> GetClosureHistoryAsync(Guid officeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.DayClosures
                .Include(d => d.Details).ThenInclude(dd => dd.Currency)
                .Include(d => d.ClosedByUser)
                .Where(d => d.OfficeId == officeId);

            if (startDate.HasValue)
                query = query.Where(d => d.BusinessDate >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(d => d.BusinessDate <= endDate.Value.Date);

            var closures = await query.OrderByDescending(d => d.BusinessDate).ToListAsync();

            return closures.Select(c => MapToViewModel(c, c.Details?.Any(d => Math.Abs(d.Discrepancy) > FinancialConstants.DiscrepancyThreshold) ?? false)).ToList();
        }

        public async Task<vm_dayclosure> GetDayClosureAsync(Guid officeId, DateTime businessDate)
        {
            var closure = await _context.DayClosures
                .Include(d => d.Details).ThenInclude(dd => dd.Currency)
                .Include(d => d.ClosedByUser)
                .FirstOrDefaultAsync(d => d.OfficeId == officeId && d.BusinessDate.Date == businessDate.Date);

            if (closure == null)
                throw new ApiException(HttpStatusCode.NotFound, "Bu tarih için kapanış kaydı bulunamadı.");

            return MapToViewModel(closure, closure.Details?.Any(d => Math.Abs(d.Discrepancy) > FinancialConstants.DiscrepancyThreshold) ?? false);
        }

        public async Task<vm_consolidated_dayclosure> GetConsolidatedDayClosureAsync(DateTime businessDate)
        {
            var date = businessDate.Date;
            var offices = await _context.Offices
                .AsNoTracking()
                .Where(o => o.IsActive)
                .OrderBy(o => o.OfficeType).ThenBy(o => o.OfficeName)
                .ToListAsync();

            var closures = await _context.DayClosures
                .AsNoTracking()
                .Include(d => d.Details)
                .Include(d => d.ClosedByUser)
                .Where(d => d.BusinessDate.Date == date)
                .ToListAsync();

            var result = new vm_consolidated_dayclosure
            {
                BusinessDate = date,
                TotalOffices = offices.Count,
                Offices = new List<vm_office_closure_summary>()
            };

            foreach (var office in offices)
            {
                var closure = closures.FirstOrDefault(c => c.OfficeId == office.Id);
                var hasDisc = closure?.Details?.Any(d => Math.Abs(d.Discrepancy) > FinancialConstants.DiscrepancyThreshold) ?? false;
                var totalDisc = closure?.Details?.Sum(d => Math.Abs(d.Discrepancy)) ?? 0;

                var summary = new vm_office_closure_summary
                {
                    OfficeId = office.Id,
                    OfficeName = office.OfficeName,
                    OfficeType = (int)office.OfficeType,
                    IsClosed = closure != null && closure.Status != DayClosureStatus.Open,
                    ClosedAt = closure?.ClosedAt,
                    ClosedByUser = closure?.ClosedByUser?.Username ?? "",
                    RealizedProfit = closure?.TotalRealizedProfit ?? 0,
                    TransactionCount = closure?.TransactionCount ?? 0,
                    HasDiscrepancy = hasDisc,
                    TotalDiscrepancyValue = totalDisc
                };

                result.Offices.Add(summary);

                if (summary.IsClosed) result.ClosedOffices++;
                else result.UnclosedOffices++;

                result.TotalRealizedProfit += summary.RealizedProfit;
                result.TotalTransactionCount += summary.TransactionCount;
                if (hasDisc) result.HasAnyDiscrepancy = true;
            }

            return result;
        }

        private async Task AutoCloseDayAsync(Guid officeId, Guid vaultId, DateTime businessDate, Guid userId)
        {
            var vault = await _context.Vaults
                .Include(v => v.Balances).ThenInclude(b => b.Currency)
                .FirstOrDefaultAsync(v => v.Id == vaultId);

            var autoClosure = new DayClosure
            {
                OfficeId = officeId,
                VaultId = vaultId,
                BusinessDate = businessDate,
                ClosedByUserId = userId,
                ClosedAt = DateTime.UtcNow,
                Status = DayClosureStatus.AutoClosed,
                TotalRealizedProfit = 0,
                TransactionCount = 0,
                Notes = "Otomatik kapanış (ara gün)",
                IsAutoGenerated = true,
                RejectionNote = "",
                Details = new List<DayClosureDetail>()
            };

            foreach (var balance in vault.Balances)
            {
                var wac = await _wacService.GetWacAsync(vaultId, balance.CurrencyId);
                autoClosure.Details.Add(new DayClosureDetail
                {
                    DayClosureId = autoClosure.Id,
                    CurrencyId = balance.CurrencyId,
                    SystemBalance = balance.Balance,
                    PhysicalCount = balance.Balance,
                    Discrepancy = 0,
                    WacAtClose = wac,
                    OpeningBalance = balance.Balance,
                    OpeningWac = wac
                });
            }

            _context.DayClosures.Add(autoClosure);
            await _context.SaveChangesAsync();
        }

        private async Task PopulateSystemBalances(vm_daystatus status, Guid officeId)
        {
            var vault = await _context.Vaults
                .Include(v => v.Balances).ThenInclude(b => b.Currency)
                .FirstOrDefaultAsync(v => v.OfficeId == officeId && v.IsActive && v.Type == Vault.VaultType.Main);

            if (vault == null) return;

            foreach (var balance in vault.Balances)
            {
                var wac = await _wacService.GetWacAsync(vault.Id, balance.CurrencyId);
                status.SystemBalances.Add(new vm_dayclosurebalance
                {
                    CurrencyId = balance.CurrencyId,
                    CurrencyCode = balance.Currency?.CurrencyCode ?? "",
                    CurrencyName = balance.Currency?.CurrencyName ?? "",
                    SystemBalance = balance.Balance,
                    CurrentWac = wac
                });
            }
        }

        // Yazdırma özeti: "kasayı açan kişi" için ayrı bir DB kaydı yok (Vault kalıcı, günlük yeniden
        // açılmıyor) — bu yüzden o iş gününün ilk işlemini yapan personel "açan kişi" olarak kabul edilir.
        private async Task PopulateActivitySummaryAsync(vm_daystatus status, Guid officeId, DateTime today, DayClosure? lastClosure)
        {
            status.LastClosedByUserName = lastClosure?.ClosedByUser != null
                ? $"{lastClosure.ClosedByUser.Firstname} {lastClosure.ClosedByUser.Lastname}"
                : null;

            // businessDate Türkiye yerel takvim günüdür, TransactionDate ise UTC olarak kaydediliyor
            // (bkz. CloseDayAsync'teki aynı düzeltme notu) — sınırlar UTC'ye çevrilmeden karşılaştırılırsa
            // TRT 00:00-03:00 arası işlemler yanlış güne düşer.
            var dayStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(today, DateTimeKind.Unspecified), TurkeyTz);
            var dayEnd = dayStart.AddDays(1);
            var todaysTransactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Details).ThenInclude(d => d.Currency)
                .Include(t => t.User)
                .Where(t => t.Vault.OfficeId == officeId &&
                            t.TransactionDate >= dayStart && t.TransactionDate < dayEnd &&
                            t.Status == TransactionStatus.Completed &&
                            !t.IsDeleted)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync();

            status.TotalTransactionCount = todaysTransactions.Count;

            var firstTransaction = todaysTransactions.FirstOrDefault();
            status.OpenedByUserName = firstTransaction?.User != null
                ? $"{firstTransaction.User.Firstname} {firstTransaction.User.Lastname}"
                : null;

            decimal totalVolume = 0;
            foreach (var transaction in todaysTransactions)
            {
                if (transaction.Type != TransactionType.Exchange && transaction.Type != TransactionType.Buy)
                    continue;

                foreach (var detail in transaction.Details.Where(d => d.Currency.CurrencyCode != "TRY"))
                {
                    totalVolume += Math.Abs(detail.Amount) * detail.Rate;
                }
            }
            status.TotalTransactionVolumeInTRY = totalVolume;
        }

        public async Task<List<vm_dayclosure>> GetPendingApprovalsAsync()
        {
            var closures = await _context.DayClosures
                .Include(d => d.Details).ThenInclude(dd => dd.Currency)
                .Include(d => d.ClosedByUser)
                .Include(d => d.Office)
                .Where(d => d.Status == DayClosureStatus.PendingApproval)
                .OrderBy(d => d.BusinessDate)
                .ToListAsync();

            return closures.Select(c => MapToViewModel(c, true)).ToList();
        }

        public async Task<vm_dayclosure> ApproveDayClosureAsync(Guid closureId, bool approve, string rejectionNote)
        {
            if (!await _validationService.IsOwnerAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Kapanış onayı için Owner yetkisi gereklidir.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var closure = await _context.DayClosures
                    .Include(d => d.Details).ThenInclude(dd => dd.Currency)
                    .Include(d => d.ClosedByUser)
                    .FirstOrDefaultAsync(d => d.Id == closureId);

                if (closure == null)
                    throw new ApiException(HttpStatusCode.NotFound, "Kapanış kaydı bulunamadı.");

                if (closure.Status != DayClosureStatus.PendingApproval)
                    throw new ApiException(HttpStatusCode.BadRequest, "Bu kapanış onay beklemiyor.");

                var userId = Guid.Parse(_validationService.GetUserID());
                var vault = await _context.Vaults.Include(v => v.Balances)
                    .FirstOrDefaultAsync(v => v.Id == closure.VaultId);

                if (approve)
                {
                    foreach (var d in closure.Details.Where(x => Math.Abs(x.Discrepancy) > FinancialConstants.DiscrepancyThreshold))
                    {
                        // Denetim raporu düzeltmesi: eskiden burada balance.Balance = d.PhysicalCount ile
                        // bakiye, kapanışın SUNULDUĞU andaki fiziksel sayıma blind olarak eşitleniyordu.
                        // Kapanış saatlerce/günlerce onay beklerken yapılmış işlemler varsa, Owner
                        // onayladığında bu işlemlerin bakiye etkisi sessizce sıfırlanıyordu. Artık sadece
                        // keşfedilen fark (Discrepancy = PhysicalCount - SubmitAnındakiSystemBalance) ŞU
                        // ANKİ bakiyeye bir DELTA olarak ekleniyor — böylece onay bekleme süresindeki
                        // işlemler korunuyor.
                        var balance = vault?.Balances.FirstOrDefault(b => b.CurrencyId == d.CurrencyId);
                        decimal newBalance;
                        if (balance != null)
                        {
                            newBalance = balance.Balance + d.Discrepancy;
                            balance.Balance = newBalance;
                            balance.LastUpdated = DateTime.UtcNow;
                        }
                        else
                        {
                            newBalance = d.Discrepancy;
                            _context.VaultBalances.Add(new VaultBalance
                            {
                                VaultId = closure.VaultId,
                                CurrencyId = d.CurrencyId,
                                Balance = newBalance,
                                LastUpdated = DateTime.UtcNow
                            });
                        }

                        await _wacService.AdjustWacQuantityAsync(closure.VaultId, d.CurrencyId, newBalance, WacAdjustReason.DayClosure);

                        _context.VaultBalanceHistories.Add(new VaultBalanceHistory
                        {
                            VaultId = closure.VaultId,
                            CurrencyId = d.CurrencyId,
                            Balance = d.Discrepancy,
                            Description = $"Gün kapanışı sayım farkı (Owner onaylı): {d.Discrepancy:+0.####;-0.####} ({closure.BusinessDate:dd.MM.yyyy})",
                            UserId = userId,
                            TransactionType = TransactionType.Adjustment,
                            IsDeleted = false,
                            IsGhost = false,
                            IsParty = false
                        });
                    }

                    closure.Status = DayClosureStatus.Closed;
                    if (vault != null)
                    {
                        vault.ShouldCount = false;
                        vault.LastCountDate = DateTime.UtcNow;
                    }
                }
                else
                {
                    closure.Status = DayClosureStatus.Rejected;
                    closure.RejectionNote = rejectionNote ?? "";
                }

                closure.ApprovedByUserId = userId;
                closure.ApprovedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToViewModel(closure, closure.Details.Any(x => Math.Abs(x.Discrepancy) > FinancialConstants.DiscrepancyThreshold));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private vm_dayclosure MapToViewModel(DayClosure closure, bool hasDiscrepancy)
        {
            return new vm_dayclosure
            {
                Id = closure.Id,
                OfficeId = closure.OfficeId,
                OfficeName = closure.Office?.OfficeName ?? "",
                BusinessDate = closure.BusinessDate,
                ClosedAt = closure.ClosedAt,
                ClosedByUser = closure.ClosedByUser?.Username ?? "",
                Status = closure.Status,
                TotalRealizedProfit = closure.TotalRealizedProfit,
                TransactionCount = closure.TransactionCount,
                HasDiscrepancy = hasDiscrepancy,
                Notes = closure.Notes,
                Details = closure.Details?.Select(d => new vm_dayclosuredetail
                {
                    CurrencyId = d.CurrencyId,
                    CurrencyCode = d.Currency?.CurrencyCode ?? "",
                    SystemBalance = d.SystemBalance,
                    PhysicalCount = d.PhysicalCount,
                    Discrepancy = d.Discrepancy,
                    DiscrepancyNote = d.DiscrepancyNote,
                    WacAtClose = d.WacAtClose,
                    OpeningBalance = d.OpeningBalance,
                    OpeningWac = d.OpeningWac
                }).ToList() ?? new List<vm_dayclosuredetail>()
            };
        }
    }
}
