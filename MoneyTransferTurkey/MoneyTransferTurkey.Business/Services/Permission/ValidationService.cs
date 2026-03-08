using Microsoft.AspNetCore.Http;
using MoneyTransferTurkey.Data.Contexts;
using MoneyTransferTurkey.Entity;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.Permission
{
    public class ValidationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MoneyTransferTurkeyDbContext _dbContext;

        public ValidationService(IHttpContextAccessor httpContextAccessor, MoneyTransferTurkeyDbContext dbContext)
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

        public async Task<bool> IsAdminAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return false;

                var dbUser = await _dbContext.Users.FindAsync(userId.Value);
                return dbUser != null && dbUser.Rank == Rank.Admin;
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
                return dbUser != null && dbUser.Rank >= Rank.Moderator;
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
