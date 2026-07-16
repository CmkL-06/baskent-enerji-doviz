using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.ExchangeOffice
{
    /// <summary>
    /// API controller for managing vault balance snapshots
    /// Provides endpoints to capture and query historical vault balances
    /// </summary>
    [Route("api/v1/exchange/vault-snapshots")]
    [ApiController]
    [Authorize]
    public class VaultSnapshotController : ControllerBase
    {
        private readonly IVaultSnapshotService _snapshotService;
        private readonly ILogger<VaultSnapshotController> _logger;
        private readonly ValidationService _permissionService;

        public VaultSnapshotController(
            IVaultSnapshotService snapshotService,
            ILogger<VaultSnapshotController> logger,
            ValidationService permissionService)
        {
            _snapshotService = snapshotService;
            _logger = logger;
            _permissionService = permissionService;
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
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "Invalid or missing user identity" });
                }

                await _permissionService.EnsureNotViewerAsync(request.OfficeId);

                var result = await _snapshotService.CreateSnapshotAsync(request, userId);
                _logger.LogInformation("Snapshot created successfully for office {OfficeId} by user {UserId}",
                    request.OfficeId, userId);

                return Ok(result);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating snapshot for office {OfficeId}", request.OfficeId);
                return BadRequest(new { error = ex.Message });
            }
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
                await _permissionService.ValidateOfficeAccessAsync(officeId);

                var result = await _snapshotService.GetSnapshotsByOfficeAsync(officeId, startDate, endDate);
                return Ok(result);
            }
            catch (ApiException)
            {
                throw;
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
                await _permissionService.ValidateOfficeAccessAsync(officeId);

                var result = await _snapshotService.GetSnapshotsByDateAsync(officeId, date);
                return Ok(result);
            }
            catch (ApiException)
            {
                throw;
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
                await _permissionService.ValidateOfficeAccessAsync(result.OfficeId);
                return Ok(result);
            }
            catch (ApiException)
            {
                throw;
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
                var snapshot = await _snapshotService.GetSnapshotByIdAsync(snapshotId);
                await _permissionService.EnsureNotViewerAsync(snapshot.OfficeId);

                var result = await _snapshotService.DeleteSnapshotAsync(snapshotId);
                if (!result)
                {
                    return NotFound(new { error = "Snapshot not found" });
                }

                _logger.LogInformation("Snapshot {SnapshotId} deleted successfully", snapshotId);
                return Ok(new { message = "Snapshot deleted successfully" });
            }
            catch (ApiException)
            {
                throw;
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
                var snapshot1 = await _snapshotService.GetSnapshotByIdAsync(snapshotId1);
                await _permissionService.ValidateOfficeAccessAsync(snapshot1.OfficeId);

                var result = await _snapshotService.CompareSnapshotsAsync(snapshotId1, snapshotId2);
                return Ok(result);
            }
            catch (ApiException)
            {
                throw;
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
