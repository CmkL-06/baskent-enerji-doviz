using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
        private readonly ValidationService _validation;
        private readonly ILogger<ExchangeHierarchyController> _logger;

        public ExchangeHierarchyController(
            IOfficeHierarchyService hierarchy,
            IOfficeTransferService transfer,
            IOfficeServiceCommand officeCommand,
            ValidationService validation,
            ILogger<ExchangeHierarchyController> logger)
        {
            _hierarchy = hierarchy;
            _transfer = transfer;
            _officeCommand = officeCommand;
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
                    return Unauthorized(new { error = "Bu işlem için Owner yetkisi gereklidir." });

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
                    return Unauthorized(new { error = "Bu işlem için Admin veya Owner yetkisi gereklidir." });

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
                    return Unauthorized(new { error = "Bu işlem için Admin veya Owner yetkisi gereklidir." });

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

        // ── Yardımcı ──────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("nameid")
                     ?? User.FindFirst("sub");
            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
        }
    }
}
