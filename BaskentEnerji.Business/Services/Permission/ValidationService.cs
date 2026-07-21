using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using System;
using System.Collections.Generic;
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

        public async Task<OfficeRole?> GetOfficeRoleAsync(Guid officeId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return null;

            return await _dbContext.User_Offices
                .Where(uo => uo.UserId == userId.Value && uo.OfficeId == officeId && uo.IsActive)
                .Select(uo => (OfficeRole?)uo.Role)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Guid>> GetAccessibleOfficeIdsAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return new List<Guid>();

            return await _dbContext.User_Offices
                .Where(uo => uo.UserId == userId.Value && uo.IsActive)
                .Select(uo => uo.OfficeId)
                .ToListAsync();
        }

        public async Task EnsureNotViewerAsync(Guid officeId)
        {
            if (await IsAdminAsync()) return; // Owner + Admin her zaman muaf

            var role = await GetOfficeRoleAsync(officeId);
            // GetOfficeRoleAsync, kullanıcının o ofisle HİÇBİR ilişkisi (User_Offices kaydı) yoksa
            // null döner — role == OfficeRole.Viewer kontrolü bu durumda false olup sessizce
            // GEÇERDİ. Yani başka bir ofiste Editor olan biri, o ofise hiç erişimi olmayan bir
            // officeId vererek bu kontrolü atlatıp o ofisin verilerini değiştirebiliyordu (IDOR).
            // Erişimi olmayan (null) kullanıcı, Viewer'dan daha az yetkiye sahip olmalı — bu yüzden
            // aynı şekilde (hatta daha sıkı) engellenmesi gerekir.
            if (role == null || role == OfficeRole.Viewer)
                throw new ApiException(HttpStatusCode.Forbidden,
                    "Bu ofis için işlem yapma yetkiniz yok.");
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out Guid userId) ? userId : (Guid?)null;
        }
    }
}
