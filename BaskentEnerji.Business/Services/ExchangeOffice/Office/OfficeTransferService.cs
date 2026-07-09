using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
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

        public OfficeTransferService(BaskentEnerjiDbContext db)
        {
            _db = db;
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

                if (model.Amount <= 0)
                    throw new ApiException(HttpStatusCode.BadRequest, "Transfer miktarı sıfırdan büyük olmalıdır.");

                // UPDLOCK to prevent concurrent transfers from passing balance check simultaneously
                var balance = await _db.VaultBalances
                    .FromSqlRaw("SELECT * FROM VaultBalances WITH (UPDLOCK) WHERE VaultId = {0} AND CurrencyId = {1}", model.SourceVaultId, model.CurrencyId)
                    .FirstOrDefaultAsync();

                if (balance == null || balance.Balance < model.Amount)
                    throw new ApiException(HttpStatusCode.BadRequest,
                        $"Kaynak kasada yeterli bakiye yok. Mevcut: {balance?.Balance ?? 0}");

                // Günlük işlem limiti kontrolü (aynı para birimi bazında)
                if (sourceVault.Office.DailyTransactionLimit.HasValue)
                {
                    var todayStart = DateTime.UtcNow.Date;
                    var dailyTotal = await _db.OfficeTransfers
                        .Where(t => t.SourceVaultId == model.SourceVaultId
                            && t.CurrencyId == model.CurrencyId
                            && t.Status == TransferStatus.Completed
                            && t.CreatedDate >= todayStart)
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
                        .Where(t => t.SourceVaultId == model.SourceVaultId
                            && t.CurrencyId == model.CurrencyId
                            && t.Status == TransferStatus.Completed
                            && t.CreatedDate >= monthStart)
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
