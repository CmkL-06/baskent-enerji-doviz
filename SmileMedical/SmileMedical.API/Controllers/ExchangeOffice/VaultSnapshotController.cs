using Microsoft.AspNetCore.Mvc;
using SmileMedical.Business.Infrastructure.ExchangeOffice.Office;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmileMedical.API.Controllers.ExchangeOffice
{
    /// <summary>
    /// API controller for managing vault balance snapshots
    /// Provides endpoints to capture and query historical vault balances
    /// </summary>
    [Route("api/v1/exchange/vault-snapshots")]
    [ApiController]
    public class VaultSnapshotController : ControllerBase
    {
        private readonly IVaultSnapshotService _snapshotService;
        private readonly ILogger<VaultSnapshotController> _logger;

        public VaultSnapshotController(
            IVaultSnapshotService snapshotService,
            ILogger<VaultSnapshotController> logger)
        {
            _snapshotService = snapshotService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a snapshot of all vault balances for a specific office
        /// POST: api/v1/exchange/vault-snapshots
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<vm_vaultbalancesnapshot>> CreateSnapshot([FromBody] rm_createsnapshot request)
        {
            try
            {
                if (!TryGetAuthenticatedUserId(out var userId))
                {
                    return Unauthorized(new { error = "Authenticated user ID claim is missing or invalid." });
                }

                var result = await _snapshotService.CreateSnapshotAsync(request, userId);
                _logger.LogInformation("Snapshot created successfully for office {OfficeId} by user {UserId}",
                    request.OfficeId, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snapshot for office {OfficeId}", request.OfficeId);
                return BadRequest(new { error = ex.Message });
            }
        }

        private bool TryGetAuthenticatedUserId(out Guid userId)
        {
            userId = Guid.Empty;

            var userIdClaim =
                User?.FindFirst("UserId")?.Value ??
                User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(userIdClaim, out userId);
        }

        /// <summary>
        /// Retrieves all snapshots for a specific office with optional date filtering
        /// GET: api/v1/exchange/vault-snapshots/office/{officeId}
        /// </summary>
        [HttpGet("office/{officeId}")]
        public async Task<ActionResult<List<vm_vaultbalancesnapshot>>> GetSnapshotsByOffice(
            Guid officeId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _snapshotService.GetSnapshotsByOfficeAsync(officeId, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving snapshots for office {OfficeId}", officeId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves snapshots for a specific date
        /// GET: api/v1/exchange/vault-snapshots/office/{officeId}/date/{date}
        /// </summary>
        [HttpGet("office/{officeId}/date/{date}")]
        public async Task<ActionResult<List<vm_vaultbalancesnapshot>>> GetSnapshotsByDate(
            Guid officeId,
            DateTime date)
        {
            try
            {
                var result = await _snapshotService.GetSnapshotsByDateAsync(officeId, date);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving snapshots for office {OfficeId} on date {Date}",
                    officeId, date);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a specific snapshot with all its details
        /// GET: api/v1/exchange/vault-snapshots/{snapshotId}
        /// </summary>
        [HttpGet("{snapshotId}")]
        public async Task<ActionResult<vm_vaultbalancesnapshot>> GetSnapshotById(Guid snapshotId)
        {
            try
            {
                var result = await _snapshotService.GetSnapshotByIdAsync(snapshotId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving snapshot {SnapshotId}", snapshotId);
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a snapshot and all its details
        /// DELETE: api/v1/exchange/vault-snapshots/{snapshotId}
        /// </summary>
        [HttpDelete("{snapshotId}")]
        public async Task<ActionResult> DeleteSnapshot(Guid snapshotId)
        {
            try
            {
                var result = await _snapshotService.DeleteSnapshotAsync(snapshotId);
                if (!result)
                {
                    return NotFound(new { error = "Snapshot not found" });
                }

                _logger.LogInformation("Snapshot {SnapshotId} deleted successfully", snapshotId);
                return Ok(new { message = "Snapshot deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting snapshot {SnapshotId}", snapshotId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Compares two snapshots to show balance changes
        /// GET: api/v1/exchange/vault-snapshots/compare
        /// </summary>
        [HttpGet("compare")]
        public async Task<ActionResult<object>> CompareSnapshots(
            [FromQuery] Guid snapshotId1,
            [FromQuery] Guid snapshotId2)
        {
            try
            {
                var result = await _snapshotService.CompareSnapshotsAsync(snapshotId1, snapshotId2);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing snapshots {SnapshotId1} and {SnapshotId2}",
                    snapshotId1, snapshotId2);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
