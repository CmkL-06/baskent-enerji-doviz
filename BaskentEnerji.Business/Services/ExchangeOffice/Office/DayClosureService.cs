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

        public async Task<vm_daystatus> GetDayStatusAsync(Guid officeId)
        {
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz).Date;

            var lastClosure = await _context.DayClosures
                .Where(d => d.OfficeId == officeId && d.Status != DayClosureStatus.Open)
                .OrderByDescending(d => d.BusinessDate)
                .FirstOrDefaultAsync();

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
                status.BlockReason = $"{firstUnclosed:dd.MM.yyyy} tarihli gün kapanışı yapılmadı. İşlem yapabilmek için önce kapanış gerekli.";
            }

            await PopulateSystemBalances(status, officeId);
            return status;
        }

        public async Task<bool> CanTransactAsync(Guid officeId)
        {
            var lastClosure = await _context.DayClosures
                .Where(d => d.OfficeId == officeId && d.Status != DayClosureStatus.Open)
                .OrderByDescending(d => d.BusinessDate)
                .FirstOrDefaultAsync();

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
                var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz).Date;
                var businessDate = request.BusinessDate.Date;

                var lastClosure = await _context.DayClosures
                    .Where(d => d.OfficeId == request.OfficeId && d.Status != DayClosureStatus.Open)
                    .OrderByDescending(d => d.BusinessDate)
                    .FirstOrDefaultAsync();

                var expectedDate = lastClosure != null ? lastClosure.BusinessDate.Date.AddDays(1) : businessDate;

                if (lastClosure != null && businessDate < expectedDate)
                    throw new ApiException(HttpStatusCode.BadRequest, "Bu tarih zaten kapatılmış.");

                if (businessDate > today)
                    throw new ApiException(HttpStatusCode.BadRequest, "Gelecek tarih kapatılamaz.");

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

                // Get transaction stats for the day
                var dayStart = businessDate;
                var dayEnd = businessDate.AddDays(1);
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
                    Notes = request.Notes,
                    IsAutoGenerated = false,
                    Details = new List<DayClosureDetail>()
                };

                bool hasAnyDiscrepancy = false;

                foreach (var detail in request.Details)
                {
                    var balance = vault.Balances.FirstOrDefault(b => b.CurrencyId == detail.CurrencyId);
                    var systemBalance = balance?.Balance ?? 0;
                    var discrepancy = detail.PhysicalCount - systemBalance;

                    if (Math.Abs(discrepancy) > 0.0001m && string.IsNullOrWhiteSpace(detail.DiscrepancyNote))
                        throw new ApiException(HttpStatusCode.BadRequest, $"Sayım farkı olan döviz için açıklama zorunludur. ({balance?.Currency?.CurrencyCode ?? detail.CurrencyId.ToString()})");

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

                    if (Math.Abs(discrepancy) > 0.0001m)
                    {
                        hasAnyDiscrepancy = true;

                        if (balance != null)
                        {
                            balance.Balance = detail.PhysicalCount;
                            balance.LastUpdated = DateTime.UtcNow;
                        }

                        await _wacService.AdjustWacQuantityAsync(vault.Id, detail.CurrencyId, detail.PhysicalCount, WacAdjustReason.DayClosure);

                        _context.VaultBalanceHistories.Add(new VaultBalanceHistory
                        {
                            VaultId = vault.Id,
                            CurrencyId = detail.CurrencyId,
                            Balance = discrepancy,
                            Description = $"Gün kapanışı sayım farkı: {discrepancy:+0.####;-0.####} ({businessDate:dd.MM.yyyy})",
                            UserId = userId,
                            TransactionType = TransactionType.Adjustment,
                            IsDeleted = false,
                            IsGhost = false,
                            IsParty = false
                        });
                    }
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

            return closures.Select(c => MapToViewModel(c, c.Details?.Any(d => Math.Abs(d.Discrepancy) > 0.0001m) ?? false)).ToList();
        }

        public async Task<vm_dayclosure> GetDayClosureAsync(Guid officeId, DateTime businessDate)
        {
            var closure = await _context.DayClosures
                .Include(d => d.Details).ThenInclude(dd => dd.Currency)
                .Include(d => d.ClosedByUser)
                .FirstOrDefaultAsync(d => d.OfficeId == officeId && d.BusinessDate.Date == businessDate.Date);

            if (closure == null)
                throw new ApiException(HttpStatusCode.NotFound, "Bu tarih için kapanış kaydı bulunamadı.");

            return MapToViewModel(closure, closure.Details?.Any(d => Math.Abs(d.Discrepancy) > 0.0001m) ?? false);
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
                var hasDisc = closure?.Details?.Any(d => Math.Abs(d.Discrepancy) > 0.0001m) ?? false;
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

        private vm_dayclosure MapToViewModel(DayClosure closure, bool hasDiscrepancy)
        {
            return new vm_dayclosure
            {
                Id = closure.Id,
                OfficeId = closure.OfficeId,
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
