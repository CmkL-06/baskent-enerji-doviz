using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using BaskentEnerji.Business.Exceptions;

namespace BaskentEnerji.Business.Services.Permission
{
    public class ValidationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly BaskentEnerjiDbContext _dbContext;

        public ValidationService(IHttpContextAccessor httpContextAccessor, BaskentEnerjiDbContext dbContext)
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

        public async Task ValidateOfficeAccessAsync(Guid officeId)
        {
            if (await IsAdminAsync()) return;

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                throw new ApiException(HttpStatusCode.Unauthorized, "Kullanıcı kimliği doğrulanamadı");

            var hasAccess = await _dbContext.User_Offices
                .AnyAsync(uo => uo.UserId == userId.Value && uo.OfficeId == officeId);

            if (!hasAccess)
                throw new ApiException(HttpStatusCode.Forbidden, "Bu ofise erişim yetkiniz yok");
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out Guid userId) ? userId : (Guid?)null;
        }
    }
}
