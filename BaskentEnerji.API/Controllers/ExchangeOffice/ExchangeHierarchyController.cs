using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.ExchangeOffice
{
    /// <summary>
    /// Ofis hiyerarşisi (Merkez/Şube/Bayi) ve ofislerarası transfer yönetimi.
    /// </summary>
    [Route("api/v1/exchange")]
    [ApiController]
    [Authorize]
    public class ExchangeHierarchyController : ControllerBase
    {
        private readonly IOfficeHierarchyService _hierarchy;
        private readonly IOfficeTransferService _transfer;
        private readonly IOfficeServiceCommand _officeCommand;
        private readonly IExchangeRateService _rateService;
        private readonly IAlertService _alertService;
        private readonly BaskentEnerjiDbContext _db;
        private readonly ValidationService _validation;
        private readonly ILogger<ExchangeHierarchyController> _logger;

        public ExchangeHierarchyController(
            IOfficeHierarchyService hierarchy,
            IOfficeTransferService transfer,
            IOfficeServiceCommand officeCommand,
            IExchangeRateService rateService,
            IAlertService alertService,
            BaskentEnerjiDbContext db,
            ValidationService validation,
            ILogger<ExchangeHierarchyController> logger)
        {
            _hierarchy = hierarchy;
            _transfer = transfer;
            _officeCommand = officeCommand;
            _rateService = rateService;
            _alertService = alertService;
            _db = db;
            _validation = validation;
            _logger = logger;
        }

        // ── Hiyerarşi ──────────────────────────────────────────

        /// <summary>Tüm ofis ağacını (Merkez → Şube/Bayi) döner.</summary>
        [HttpGet("offices/hierarchy")]
        public async Task<ActionResult<List<vm_office>>> GetHierarchy()
        {
            try
            {
                var result = await _hierarchy.GetHierarchyAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting office hierarchy");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>Merkez ofisini döner.</summary>
        [HttpGet("offices/merkez")]
        public async Task<ActionResult<vm_office>> GetMerkez()
        {
            var result = await _hierarchy.GetMerkezAsync();
            if (result == null) return NotFound(new { error = "Merkez ofis tanımlı değil." });
            return Ok(result);
        }

        /// <summary>Alt birimleri (şube/bayi) listeler.</summary>
        [HttpGet("offices/{parentId}/children")]
        public async Task<ActionResult<List<vm_office>>> GetChildren(Guid parentId)
        {
            var result = await _hierarchy.GetChildrenAsync(parentId);
            return Ok(result);
        }

        /// <summary>Giriş yapan kullanıcının erişebileceği ofisler (ofis rolüyle birlikte).</summary>
        [HttpGet("offices/my-access")]
        public async Task<ActionResult<List<vm_useroffice>>> GetMyOffices()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _hierarchy.GetUserAccessibleOfficesAsync(userId);
            return Ok(result);
        }

        /// <summary>Yeni ofis/şube/bayi ekle veya güncelle.</summary>
        [HttpPost("office")]
        public async Task<IActionResult> SaveOffice([FromBody] rm_saveoffice data)
        {
            try
            {
                if (!await _validation.IsOwnerAsync())
                    return StatusCode(403, new { error = "Bu işlem için Owner yetkisi gereklidir." });

                await _officeCommand.SaveOffice(data);
                return Ok(new { message = "Ofis kaydedildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving office");
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Transfer ───────────────────────────────────────────

        /// <summary>
        /// Transfer talebi oluştur (Şube/Bayi → Merkez onayına gönderir).
        /// </summary>
        [HttpPost("office-transfer/request")]
        public async Task<ActionResult<vm_officetransfer>> RequestTransfer(
            [FromBody] rm_create_officetransfer model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                var result = await _transfer.CreateTransferRequestAsync(model, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transfer request");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Bekleyen transfer taleplerini listele (Merkez / Admin görür).
        /// </summary>
        [HttpGet("office-transfer/pending")]
        public async Task<ActionResult<List<vm_officetransfer>>> GetPendingTransfers()
        {
            try
            {
                if (!await _validation.IsAdminAsync())
                    return StatusCode(403, new { error = "Bu işlem için Admin veya Owner yetkisi gereklidir." });

                var result = await _transfer.GetPendingTransfersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending transfers");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Belirli bir ofise ait transfer geçmişi.
        /// </summary>
        [HttpGet("office-transfer/office/{officeId}")]
        public async Task<ActionResult<List<vm_officetransfer>>> GetTransfersByOffice(Guid officeId)
        {
            try
            {
                var result = await _transfer.GetTransfersByOfficeAsync(officeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transfers by office");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Transfer onayla veya reddet (Merkez / Admin yetkisi).
        /// </summary>
        [HttpPost("office-transfer/{transferId}/action")]
        public async Task<ActionResult<vm_officetransfer>> ProcessTransfer(
            Guid transferId,
            [FromBody] rm_action_officetransfer action)
        {
            try
            {
                if (!await _validation.IsAdminAsync())
                    return StatusCode(403, new { error = "Bu işlem için Admin veya Owner yetkisi gereklidir." });

                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                var result = await _transfer.ProcessTransferAsync(transferId, action, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transfer");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Transfer detayı.</summary>
        [HttpGet("office-transfer/{transferId}")]
        public async Task<ActionResult<vm_officetransfer>> GetTransfer(Guid transferId)
        {
            var result = await _transfer.GetTransferByIdAsync(transferId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // ── Performans ─────────────────────────────────────────

        /// <summary>Şube performans karşılaştırması.</summary>
        [HttpGet("reports/branch-comparison")]
        public async Task<IActionResult> GetBranchComparison([FromQuery] string period = "daily")
        {
            try
            {
                if (!await _validation.IsAdminAsync()) return StatusCode(403, new { error = "Yetersiz yetki." });

                var startDate = period switch
                {
                    "weekly" => DateTime.Today.AddDays(-7),
                    "monthly" => DateTime.Today.AddMonths(-1),
                    _ => DateTime.Today
                };

                var offices = await _db.Offices.AsNoTracking()
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.OfficeType).ThenBy(o => o.OfficeName)
                    .ToListAsync();

                var result = new List<object>();

                foreach (var office in offices)
                {
                    var vaultIds = await _db.Vaults.AsNoTracking()
                        .Where(v => v.OfficeId == office.Id && v.IsActive)
                        .Select(v => v.Id)
                        .ToListAsync();

                    var txCount = await _db.Transactions.AsNoTracking()
                        .Where(t => vaultIds.Contains(t.VaultId) && t.CreatedDate >= startDate)
                        .CountAsync();

                    var totalVolume = await _db.TransactionDetails.AsNoTracking()
                        .Where(d => vaultIds.Contains(d.Transaction.VaultId) && d.Transaction.CreatedDate >= startDate)
                        .SumAsync(d => (decimal?)d.Amount) ?? 0;

                    var totalProfit = await _db.Transactions.AsNoTracking()
                        .Where(t => vaultIds.Contains(t.VaultId) && t.CreatedDate >= startDate)
                        .SumAsync(t => (decimal?)t.Profit) ?? 0;

                    var transferCount = await _db.OfficeTransfers.AsNoTracking()
                        .Where(t => (t.SourceVault.OfficeId == office.Id || t.TargetVault.OfficeId == office.Id) &&
                                   t.CreatedDate >= startDate)
                        .CountAsync();

                    result.Add(new
                    {
                        officeId = office.Id,
                        officeName = office.OfficeName,
                        officeType = (int)office.OfficeType,
                        period,
                        transactionCount = txCount,
                        totalVolume,
                        totalProfit,
                        transferCount
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch comparison");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ── Uyarılar ──────────────────────────────────────────

        /// <summary>Okunmamış uyarıları listele.</summary>
        [HttpGet("alerts/unread")]
        public async Task<IActionResult> GetUnreadAlerts()
        {
            if (!await _validation.IsAdminAsync()) return StatusCode(403, new { error = "Yetersiz yetki." });
            var alerts = await _alertService.GetUnreadAlertsAsync();
            return Ok(alerts);
        }

        /// <summary>Ofise ait uyarıları listele.</summary>
        [HttpGet("alerts/office/{officeId}")]
        public async Task<IActionResult> GetAlertsByOffice(Guid officeId, [FromQuery] int limit = 50)
        {
            var alerts = await _alertService.GetAlertsByOfficeAsync(officeId, limit);
            return Ok(alerts);
        }

        /// <summary>Uyarıyı okundu işaretle.</summary>
        [HttpPost("alerts/{alertId}/read")]
        public async Task<IActionResult> MarkAlertRead(Guid alertId)
        {
            var userId = GetCurrentUserId();
            await _alertService.MarkAsReadAsync(alertId, userId);
            return Ok(new { message = "Uyarı okundu olarak işaretlendi." });
        }

        /// <summary>Tüm uyarıları okundu işaretle.</summary>
        [HttpPost("alerts/read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetCurrentUserId();
            await _alertService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "Tüm uyarılar okundu." });
        }

        /// <summary>Uyarıyı çözüldü işaretle.</summary>
        [HttpPost("alerts/{alertId}/resolve")]
        public async Task<IActionResult> ResolveAlert(Guid alertId)
        {
            await _alertService.ResolveAlertAsync(alertId);
            return Ok(new { message = "Uyarı çözüldü olarak işaretlendi." });
        }

        // ── Kur Yönetimi ──────────────────────────────────────

        /// <summary>Merkez kurlarını UseParent modundaki şubelere kopyala.</summary>
        [HttpPost("rates/push-to-branches")]
        public async Task<IActionResult> PushRatesToBranches([FromBody] PushRatesRequest request)
        {
            try
            {
                if (!await _validation.IsOwnerAsync())
                    return StatusCode(403, new { error = "Bu işlem için Owner yetkisi gereklidir." });

                var count = await _rateService.PushRatesToBranchesAsync(request.MerkezOfficeId);
                return Ok(new { message = $"{count} kur güncellendi.", updatedCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing rates to branches");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Ofis için geçerli kuru döner (fallback: parent → global).</summary>
        [HttpGet("rates/effective/{officeId}/{sourceCurrencyId}/{targetCurrencyId}")]
        public async Task<IActionResult> GetEffectiveRate(Guid officeId, Guid sourceCurrencyId, Guid targetCurrencyId)
        {
            var rate = await _rateService.GetEffectiveRateAsync(officeId, sourceCurrencyId, targetCurrencyId);
            if (rate == null) return NotFound(new { error = "Bu para birimi çifti için kur bulunamadı." });
            return Ok(rate);
        }

        // ── Yardımcı ──────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("nameid")
                     ?? User.FindFirst("sub");
            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
        }
    }

    public class PushRatesRequest
    {
        public Guid MerkezOfficeId { get; set; }
    }
}
