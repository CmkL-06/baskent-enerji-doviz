using Microsoft.AspNetCore.Http;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.Permission
{
    public class ValidationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AnasıTAS_DenizDbContext _dbContext;

        public ValidationService(IHttpContextAccessor httpContextAccessor, AnasıTAS_DenizDbContext dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
        }

        public string GetUserID()
        {
            return GetCurrentUserId()?.ToString();
        }

        public async Task<bool> HasPermissionAsync(Guid userId)
        {
            return await IsAdminAsync() || HasPermissionToChange(userId);
        }

        public bool HasPermissionToChange(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            return currentUserId.HasValue && currentUserId.Value == userId;
        }

        public async Task<bool> IsOwnerAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return false;

                var dbUser = await _dbContext.Users.FindAsync(userId.Value);
                return dbUser != null && dbUser.Rank == Rank.Owner;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> IsAdminAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return false;

                var dbUser = await _dbContext.Users.FindAsync(userId.Value);
                return dbUser != null && (dbUser.Rank == Rank.Admin || dbUser.Rank == Rank.Owner);
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> IsStaff()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return false;

                var dbUser = await _dbContext.Users.FindAsync(userId.Value);
                return dbUser != null && dbUser.Rank >= Rank.Staff;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out Guid userId) ? userId : (Guid?)null;
        }
    }
}
