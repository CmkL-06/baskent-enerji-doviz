using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SmileMedical.API.Controllers.ExchangeOffice;
using SmileMedical.Business.Infrastructure.ExchangeOffice.Office;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using Xunit;

namespace SmileMedical.API.Tests;

public class VaultSnapshotControllerTests
{
    [Fact]
    public async Task CreateSnapshot_WithUserIdClaim_UsesAuthenticatedUserId()
    {
        var expectedUserId = Guid.NewGuid();
        var service = new CapturingVaultSnapshotService();
        var controller = BuildController(service, new Claim("UserId", expectedUserId.ToString()));

        var actionResult = await controller.CreateSnapshot(new rm_createsnapshot { OfficeId = Guid.NewGuid() });

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.IsType<vm_vaultbalancesnapshot>(ok.Value);
        Assert.Equal(expectedUserId, service.LastUserId);
    }

    [Fact]
    public async Task CreateSnapshot_WithoutUserIdClaim_FallsBackToNameIdentifier()
    {
        var expectedUserId = Guid.NewGuid();
        var service = new CapturingVaultSnapshotService();
        var controller = BuildController(service, new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()));

        var actionResult = await controller.CreateSnapshot(new rm_createsnapshot { OfficeId = Guid.NewGuid() });

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.IsType<vm_vaultbalancesnapshot>(ok.Value);
        Assert.Equal(expectedUserId, service.LastUserId);
    }

    [Fact]
    public async Task CreateSnapshot_WithoutValidUserClaim_ReturnsUnauthorized()
    {
        var service = new CapturingVaultSnapshotService();
        var controller = BuildController(service);

        var actionResult = await controller.CreateSnapshot(new rm_createsnapshot { OfficeId = Guid.NewGuid() });

        Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
        Assert.False(service.CreateSnapshotCalled);
    }

    private static VaultSnapshotController BuildController(
        IVaultSnapshotService service,
        params Claim[] claims)
    {
        var identity = claims.Length > 0
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();

        return new VaultSnapshotController(service, NullLogger<VaultSnapshotController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private sealed class CapturingVaultSnapshotService : IVaultSnapshotService
    {
        public Guid LastUserId { get; private set; }
        public bool CreateSnapshotCalled { get; private set; }

        public Task<vm_vaultbalancesnapshot> CreateSnapshotAsync(rm_createsnapshot data, Guid userId)
        {
            CreateSnapshotCalled = true;
            LastUserId = userId;

            return Task.FromResult(new vm_vaultbalancesnapshot
            {
                Id = Guid.NewGuid(),
                OfficeId = data.OfficeId,
                UserId = userId,
                OfficeName = "Test Office",
                UserName = "Test User",
                SnapshotDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                Details = new List<vm_vaultbalancesnapshotdetail>()
            });
        }

        public Task<List<vm_vaultbalancesnapshot>> GetSnapshotsByOfficeAsync(Guid officeId, DateTime? startDate = null, DateTime? endDate = null)
            => throw new NotImplementedException();

        public Task<vm_vaultbalancesnapshot> GetSnapshotByIdAsync(Guid snapshotId)
            => throw new NotImplementedException();

        public Task<List<vm_vaultbalancesnapshot>> GetSnapshotsByDateAsync(Guid officeId, DateTime date)
            => throw new NotImplementedException();

        public Task<bool> DeleteSnapshotAsync(Guid snapshotId)
            => throw new NotImplementedException();

        public Task<object> CompareSnapshotsAsync(Guid snapshotId1, Guid snapshotId2)
            => throw new NotImplementedException();
    }
}
