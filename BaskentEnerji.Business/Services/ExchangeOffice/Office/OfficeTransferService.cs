using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
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
    public class OfficeTransferService : IOfficeTransferService
    {
        private readonly BaskentEnerjiDbContext _db;
        private readonly ValidationService _validationService;
        private readonly IWacService _wacService;

        public OfficeTransferService(BaskentEnerjiDbContext db, ValidationService validationService, IWacService wacService)
        {
            _db = db;
            _validationService = validationService;
            _wacService = wacService;
        }

        public async Task<vm_officetransfer> CreateTransferRequestAsync(
            rm_create_officetransfer model, Guid requestedByUserId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var sourceVault = await _db.Vaults
                    .Include(v => v.Office)
                    .FirstOrDefaultAsync(v => v.Id == model.SourceVaultId)
                    ?? throw new ApiException(HttpStatusCode.NotFound, "Kaynak kasa bulunamadı.");

                var targetVault = await _db.Vaults
                    .Include(v => v.Office)
                    .FirstOrDefaultAsync(v => v.Id == model.TargetVaultId)
                    ?? throw new ApiException(HttpStatusCode.NotFound, "Hedef kasa bulunamadı.");

                await _validationService.EnsureNotViewerAsync(sourceVault.OfficeId);

                if (model.Amount <= 0)
                    throw new ApiException(HttpStatusCode.BadRequest, "Transfer miktarı sıfırdan büyük olmalıdır.");

                // UPDLOCK to prevent concurrent transfers from passing balance check simultaneously
                var balance = await _db.VaultBalances
                    .FromSqlRaw("SELECT * FROM VaultBalances WITH (UPDLOCK) WHERE VaultId = {0} AND CurrencyId = {1}", model.SourceVaultId, model.CurrencyId)
                    .FirstOrDefaultAsync();

                if (balance == null || balance.Balance < model.Amount)
                    throw new ApiException(HttpStatusCode.BadRequest,
                        $"Kaynak kasada yeterli bakiye yok. Mevcut: {balance?.Balance ?? 0}");

                // Günlük işlem limiti kontrolü (aynı para birimi bazında). Sertleştirme: bu sorgu da
                // UPDLOCK ile okunuyor — az önceki bakiye UPDLOCK'u aynı transaction içinde zaten
                // eşzamanlı transferleri serileştiriyor (test ile doğrulandı), ancak bu ek kilit,
                // ileride bakiye kontrolünün kaldırılması/değişmesi durumunda dahi bu kontrolün
                // kendi başına race'e açık kalmamasını garantiler.
                if (sourceVault.Office.DailyTransactionLimit.HasValue)
                {
                    var todayStart = DateTime.UtcNow.Date;
                    var dailyTotal = await _db.OfficeTransfers
                        .FromSqlRaw("SELECT * FROM OfficeTransfers WITH (UPDLOCK) WHERE SourceVaultId = {0} AND CurrencyId = {1} AND Status = {2} AND CreatedDate >= {3}",
                            model.SourceVaultId, model.CurrencyId, (int)TransferStatus.Completed, todayStart)
                        .SumAsync(t => t.Amount);

                    if (dailyTotal + model.Amount > sourceVault.Office.DailyTransactionLimit.Value)
                        throw new ApiException(HttpStatusCode.BadRequest,
                            $"Günlük işlem limiti aşıldı. Limit: {sourceVault.Office.DailyTransactionLimit.Value:N0}, Bugünkü toplam: {dailyTotal:N0}");
                }

                // Aylık işlem limiti kontrolü (aynı para birimi bazında)
                if (sourceVault.Office.MonthlyTransactionLimit.HasValue)
                {
                    var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var monthlyTotal = await _db.OfficeTransfers
                        .FromSqlRaw("SELECT * FROM OfficeTransfers WITH (UPDLOCK) WHERE SourceVaultId = {0} AND CurrencyId = {1} AND Status = {2} AND CreatedDate >= {3}",
                            model.SourceVaultId, model.CurrencyId, (int)TransferStatus.Completed, monthStart)
                        .SumAsync(t => t.Amount);

                    if (monthlyTotal + model.Amount > sourceVault.Office.MonthlyTransactionLimit.Value)
                        throw new ApiException(HttpStatusCode.BadRequest,
                            $"Aylık işlem limiti aşıldı. Limit: {sourceVault.Office.MonthlyTransactionLimit.Value:N0}, Bu ayki toplam: {monthlyTotal:N0}");
                }

                // Eşik üstü transferler manuel onaya düşer, eşik altı/eşik tanımsızsa otomatik tamamlanır
                var threshold = sourceVault.Office.TransferApprovalThreshold;
                var requiresApproval = threshold.HasValue && model.Amount > threshold.Value;

                var transfer = new OfficeTransfer
                {
                    Id = Guid.NewGuid(),
                    CreatedDate = DateTime.UtcNow,
                    SourceVaultId = model.SourceVaultId,
                    TargetVaultId = model.TargetVaultId,
                    CurrencyId = model.CurrencyId,
                    Amount = model.Amount,
                    Notes = model.Notes,
                    RequestedByUserId = requestedByUserId,
                    Status = requiresApproval ? TransferStatus.Pending : TransferStatus.Completed
                };

                if (requiresApproval)
                {
                    transfer.Notes = (transfer.Notes ?? "") + $" [Onay bekliyor — eşik {threshold!.Value:N0} aşıldı]";
                }
                else
                {
                    transfer.ApprovedByUserId = requestedByUserId;
                    transfer.ProcessedAt = DateTime.UtcNow;
                    transfer.Notes = (transfer.Notes ?? "") + " [Otomatik transfer]";

                    balance.Balance -= model.Amount;

                    var targetBalance = await _db.VaultBalances
                        .FirstOrDefaultAsync(b => b.VaultId == model.TargetVaultId && b.CurrencyId == model.CurrencyId);
                    if (targetBalance == null)
                    {
                        targetBalance = new VaultBalance
                        {
                            Id = Guid.NewGuid(),
                            VaultId = model.TargetVaultId,
                            CurrencyId = model.CurrencyId,
                            Balance = 0,
                            CreatedDate = DateTime.UtcNow
                        };
                        await _db.VaultBalances.AddAsync(targetBalance);
                    }
                    targetBalance.Balance += model.Amount;

                    // VaultService.VoidVaultBalanceHistoryAsync bakiyeleri VaultBalanceHistory
                    // kayıtlarını replay ederek yeniden hesaplıyor — bu tabloya yazılmayan bir
                    // hareket, aynı kasada başka bir hareket void edildiğinde sessizce kaybolur.
                    // Transfer tutarları da diğer tüm bakiye değişiklikleri gibi buraya yazılmalı.
                    await _db.VaultBalanceHistories.AddRangeAsync(
                        new VaultBalanceHistory
                        {
                            Id = Guid.NewGuid(),
                            VaultId = model.SourceVaultId,
                            CurrencyId = model.CurrencyId,
                            Balance = -model.Amount,
                            Description = $"Şube transferi (giden) → {targetVault.Office?.OfficeName ?? targetVault.Name}",
                            UserId = requestedByUserId,
                            TransactionType = TransactionType.Transfer,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false,
                            IsGhost = false,
                            IsParty = false
                        },
                        new VaultBalanceHistory
                        {
                            Id = Guid.NewGuid(),
                            VaultId = model.TargetVaultId,
                            CurrencyId = model.CurrencyId,
                            Balance = model.Amount,
                            Description = $"Şube transferi (gelen) ← {sourceVault.Office?.OfficeName ?? sourceVault.Name}",
                            UserId = requestedByUserId,
                            TransactionType = TransactionType.Transfer,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false,
                            IsGhost = false,
                            IsParty = false
                        });
                }

                await _db.OfficeTransfers.AddAsync(transfer);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return await GetTransferByIdAsync(transfer.Id)
                    ?? throw new ApiException(HttpStatusCode.InternalServerError, "Transfer oluşturulamadı.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<vm_officetransfer> ProcessTransferAsync(
            Guid transferId, rm_action_officetransfer action, Guid approvedByUserId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var transfer = await _db.OfficeTransfers
                    .Include(t => t.SourceVault).ThenInclude(v => v.Office)
                    .Include(t => t.TargetVault).ThenInclude(v => v.Office)
                    .FirstOrDefaultAsync(t => t.Id == transferId)
                    ?? throw new ApiException(HttpStatusCode.NotFound, "Transfer bulunamadı.");

                if (transfer.Status != TransferStatus.Pending)
                    throw new ApiException(HttpStatusCode.BadRequest, "Bu transfer zaten işlenmiş.");

                if (action.Approve)
                {
                    var sourceBalance = await _db.VaultBalances
                        .FromSqlRaw("SELECT * FROM VaultBalances WITH (UPDLOCK) WHERE VaultId = {0} AND CurrencyId = {1}", transfer.SourceVaultId, transfer.CurrencyId)
                        .FirstOrDefaultAsync();

                    if (sourceBalance == null || sourceBalance.Balance < transfer.Amount)
                        throw new ApiException(HttpStatusCode.BadRequest, "Kaynak kasada yeterli bakiye kalmadı.");

                    // Sertleştirme: talep oluşturulduğunda (eşik üstü olduğu için onaya düştüğünde)
                    // günlük/aylık limit kontrol edilmişti, ama onay anına kadar geçen sürede aynı
                    // kasadan başka otomatik transferler gerçekleşmiş olabilir. Onay anında limitler
                    // tekrar kontrol edilmezse eşik atlatılabilirdi.
                    var office = transfer.SourceVault.Office;
                    if (office.DailyTransactionLimit.HasValue)
                    {
                        var todayStart = DateTime.UtcNow.Date;
                        var dailyTotal = await _db.OfficeTransfers
                            .FromSqlRaw("SELECT * FROM OfficeTransfers WITH (UPDLOCK) WHERE SourceVaultId = {0} AND CurrencyId = {1} AND Status = {2} AND CreatedDate >= {3}",
                                transfer.SourceVaultId, transfer.CurrencyId, (int)TransferStatus.Completed, todayStart)
                            .SumAsync(t => t.Amount);

                        if (dailyTotal + transfer.Amount > office.DailyTransactionLimit.Value)
                            throw new ApiException(HttpStatusCode.BadRequest,
                                $"Günlük işlem limiti onay anında aşıldı. Limit: {office.DailyTransactionLimit.Value:N0}, Bugünkü toplam: {dailyTotal:N0}");
                    }

                    if (office.MonthlyTransactionLimit.HasValue)
                    {
                        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        var monthlyTotal = await _db.OfficeTransfers
                            .FromSqlRaw("SELECT * FROM OfficeTransfers WITH (UPDLOCK) WHERE SourceVaultId = {0} AND CurrencyId = {1} AND Status = {2} AND CreatedDate >= {3}",
                                transfer.SourceVaultId, transfer.CurrencyId, (int)TransferStatus.Completed, monthStart)
                            .SumAsync(t => t.Amount);

                        if (monthlyTotal + transfer.Amount > office.MonthlyTransactionLimit.Value)
                            throw new ApiException(HttpStatusCode.BadRequest,
                                $"Aylık işlem limiti onay anında aşıldı. Limit: {office.MonthlyTransactionLimit.Value:N0}, Bu ayki toplam: {monthlyTotal:N0}");
                    }

                    sourceBalance.Balance -= transfer.Amount;

                    var targetBalance = await _db.VaultBalances
                        .FirstOrDefaultAsync(b => b.VaultId == transfer.TargetVaultId && b.CurrencyId == transfer.CurrencyId);

                    if (targetBalance == null)
                    {
                        targetBalance = new VaultBalance
                        {
                            Id = Guid.NewGuid(),
                            VaultId = transfer.TargetVaultId,
                            CurrencyId = transfer.CurrencyId,
                            Balance = 0,
                            CreatedDate = DateTime.UtcNow
                        };
                        await _db.VaultBalances.AddAsync(targetBalance);
                    }

                    targetBalance.Balance += transfer.Amount;

                    // Denetim bulgusu: transfer edilen para biriminin WAC (ortalama maliyet) kaydı
                    // hiç güncellenmiyordu. Kaynak kasanın CurrencyWacs miktarı gerçek VaultBalance'tan
                    // sessizce sapıyor, hedef kasa bu para birimini ilk kez alıyorsa maliyet tabanı
                    // hiç oluşmuyordu (WAC=0) — bu da sonraki bir satışta kârın yanlışlıkla 0
                    // görünmesine yol açıyordu (bkz. CalculateRealizedProfitAsync'teki WAC=0 durumu).
                    var transferCurrency = await _db.Currencies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == transfer.CurrencyId);
                    if (transferCurrency != null && transferCurrency.CurrencyCode != "TRY")
                    {
                        var sourceWac = await _wacService.GetWacAsync(transfer.SourceVaultId, transfer.CurrencyId);

                        // Kaynak: miktar azalır, maliyet tabanı (WAC) değişmez — fiziksel yer
                        // değiştirme, yeni bir satış değil.
                        await _wacService.AdjustWacQuantityAsync(transfer.SourceVaultId, transfer.CurrencyId, sourceBalance.Balance, WacAdjustReason.Transfer, transfer.Id);

                        if (sourceWac > 0)
                        {
                            // Hedef: kaynağın maliyet tabanı biliniyorsa gerçek bir "alış" gibi işlenip
                            // hedefin mevcut maliyetiyle ağırlıklı ortalaması alınır.
                            await _wacService.RecalculateWacOnPurchaseAsync(transfer.TargetVaultId, transfer.CurrencyId, transfer.Amount, sourceWac, transfer.Id);
                        }
                        else
                        {
                            // Kaynağın da maliyet tabanı yoksa (WAC=0), hedefin mevcut WAC'ını
                            // yanlışlıkla sıfıra doğru sulandırmamak için sadece miktar eklenir.
                            var targetWacRow = await _db.CurrencyWacs.AsNoTracking()
                                .FirstOrDefaultAsync(w => w.VaultId == transfer.TargetVaultId && w.CurrencyId == transfer.CurrencyId);
                            var newTargetQty = (targetWacRow?.Quantity ?? 0) + transfer.Amount;
                            await _wacService.AdjustWacQuantityAsync(transfer.TargetVaultId, transfer.CurrencyId, newTargetQty, WacAdjustReason.Transfer, transfer.Id);
                        }
                    }

                    await _db.VaultBalanceHistories.AddRangeAsync(
                        new VaultBalanceHistory
                        {
                            Id = Guid.NewGuid(),
                            VaultId = transfer.SourceVaultId,
                            CurrencyId = transfer.CurrencyId,
                            Balance = -transfer.Amount,
                            Description = $"Şube transferi (giden, onaylandı) → {transfer.TargetVault.Office?.OfficeName ?? transfer.TargetVault.Name}",
                            UserId = approvedByUserId,
                            TransactionType = TransactionType.Transfer,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false,
                            IsGhost = false,
                            IsParty = false
                        },
                        new VaultBalanceHistory
                        {
                            Id = Guid.NewGuid(),
                            VaultId = transfer.TargetVaultId,
                            CurrencyId = transfer.CurrencyId,
                            Balance = transfer.Amount,
                            Description = $"Şube transferi (gelen, onaylandı) ← {transfer.SourceVault.Office?.OfficeName ?? transfer.SourceVault.Name}",
                            UserId = approvedByUserId,
                            TransactionType = TransactionType.Transfer,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false,
                            IsGhost = false,
                            IsParty = false
                        });

                    transfer.Status = TransferStatus.Completed;
                }
                else
                {
                    transfer.Status = TransferStatus.Rejected;
                    transfer.RejectionReason = action.RejectionReason;
                }

                transfer.ApprovedByUserId = approvedByUserId;
                transfer.ProcessedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return await GetTransferByIdAsync(transferId)
                    ?? throw new ApiException(HttpStatusCode.InternalServerError, "Transfer güncellenemedi.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<vm_officetransfer>> GetPendingTransfersAsync()
        {
            var transfers = await _db.OfficeTransfers
                .Include(t => t.SourceVault).ThenInclude(v => v.Office)
                .Include(t => t.TargetVault).ThenInclude(v => v.Office)
                .Include(t => t.Currency)
                .Include(t => t.RequestedBy)
                .Include(t => t.ApprovedBy)
                .Where(t => t.Status == TransferStatus.Pending)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return transfers.Select(MapToVm).ToList();
        }

        public async Task<List<vm_officetransfer>> GetTransfersByOfficeAsync(Guid officeId)
        {
            var transfers = await _db.OfficeTransfers
                .Include(t => t.SourceVault).ThenInclude(v => v.Office)
                .Include(t => t.TargetVault).ThenInclude(v => v.Office)
                .Include(t => t.Currency)
                .Include(t => t.RequestedBy)
                .Include(t => t.ApprovedBy)
                .Where(t => t.SourceVault.OfficeId == officeId || t.TargetVault.OfficeId == officeId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return transfers.Select(MapToVm).ToList();
        }

        public async Task<vm_officetransfer?> GetTransferByIdAsync(Guid transferId)
        {
            var t = await _db.OfficeTransfers
                .Include(t => t.SourceVault).ThenInclude(v => v.Office)
                .Include(t => t.TargetVault).ThenInclude(v => v.Office)
                .Include(t => t.Currency)
                .Include(t => t.RequestedBy)
                .Include(t => t.ApprovedBy)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            return t == null ? null : MapToVm(t);
        }

        private static vm_officetransfer MapToVm(OfficeTransfer t) => new()
        {
            Id = t.Id,
            CreatedDate = t.CreatedDate,
            SourceVaultId = t.SourceVaultId,
            SourceVaultName = t.SourceVault?.Name ?? "",
            SourceOfficeName = t.SourceVault?.Office?.OfficeName ?? "",
            TargetVaultId = t.TargetVaultId,
            TargetVaultName = t.TargetVault?.Name ?? "",
            TargetOfficeName = t.TargetVault?.Office?.OfficeName ?? "",
            CurrencyCode = t.Currency?.CurrencyCode ?? "",
            Amount = t.Amount,
            Status = t.Status.ToString(),
            Notes = t.Notes,
            RejectionReason = t.RejectionReason,
            RequestedByName = t.RequestedBy != null
                ? $"{t.RequestedBy.Firstname} {t.RequestedBy.Lastname}".Trim()
                : "",
            ApprovedByName = t.ApprovedBy != null
                ? $"{t.ApprovedBy.Firstname} {t.ApprovedBy.Lastname}".Trim()
                : null,
            ProcessedAt = t.ProcessedAt
        };
    }
}
