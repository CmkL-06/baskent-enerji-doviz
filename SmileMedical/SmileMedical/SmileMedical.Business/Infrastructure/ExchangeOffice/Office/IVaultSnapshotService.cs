using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.ExchangeOffice.Office
{
    /// <summary>
    /// Service interface for managing vault balance snapshots
    /// Provides functionality to capture and query historical vault balances
    /// </summary>
    public interface IVaultSnapshotService
    {
        /// <summary>
        /// Creates a snapshot of all vault balances for a specific office
        /// </summary>
        /// <param name="data">Snapshot creation request containing office ID and optional description</param>
        /// <param name="userId">ID of the user creating the snapshot</param>
        /// <returns>The created snapshot with details</returns>
        Task<vm_vaultbalancesnapshot> CreateSnapshotAsync(rm_createsnapshot data, Guid userId);

        /// <summary>
        /// Retrieves all snapshots for a specific office
        /// </summary>
        /// <param name="officeId">Office ID to filter snapshots</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>List of snapshots without details</returns>
        Task<List<vm_vaultbalancesnapshot>> GetSnapshotsByOfficeAsync(Guid officeId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Retrieves a specific snapshot with all its details
        /// </summary>
        /// <param name="snapshotId">Snapshot ID</param>
        /// <returns>Snapshot with all vault balance details</returns>
        Task<vm_vaultbalancesnapshot> GetSnapshotByIdAsync(Guid snapshotId);

        /// <summary>
        /// Retrieves snapshots for a specific date
        /// </summary>
        /// <param name="officeId">Office ID to filter snapshots</param>
        /// <param name="date">Specific date to query</param>
        /// <returns>Snapshots for the specified date</returns>
        Task<List<vm_vaultbalancesnapshot>> GetSnapshotsByDateAsync(Guid officeId, DateTime date);

        /// <summary>
        /// Deletes a snapshot and all its details
        /// </summary>
        /// <param name="snapshotId">Snapshot ID to delete</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteSnapshotAsync(Guid snapshotId);

        /// <summary>
        /// Compares two snapshots to show balance changes
        /// </summary>
        /// <param name="snapshotId1">First snapshot ID</param>
        /// <param name="snapshotId2">Second snapshot ID</param>
        /// <returns>Comparison result showing differences</returns>
        Task<object> CompareSnapshotsAsync(Guid snapshotId1, Guid snapshotId2);
    }
}
