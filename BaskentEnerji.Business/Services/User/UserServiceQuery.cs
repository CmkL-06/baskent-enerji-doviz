using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.User;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Modals.RequestModals.User;
using BaskentEnerji.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.User
{
    public class UserServiceQuery : IUserServiceQuery
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly IMapper mapper;
        private readonly ValidationService _validationService;

        public UserServiceQuery(BaskentEnerjiDbContext dbContext, IMapper mapper, ValidationService validationService)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
            _validationService = validationService;
        }
  
        public async Task<vm_user> GetUser(rm_user_get FilterData)
        {
            // IsStaff() herhangi bir personelin ID/kullanıcı adı/e-posta vererek BAŞKA bir
            // kullanıcının (Owner/Admin dahil) tam profilini çekmesine izin veriyordu (IDOR).
            // Bu endpoint şu an kod tabanında yalnızca Admin panelinden kullanılıyor — kendi
            // profilini görüntüleme ihtiyacı yok, bu yüzden Admin/Owner ile sınırlanıyor.
            if (!await _validationService.IsAdminAsync()) throw new ApiException(HttpStatusCode.Forbidden, "You have no permission to do this.");

            var query =  _dbContext.Users.AsQueryable();

            if (FilterData == null) throw new ApiException(HttpStatusCode.NotAcceptable, "");

            if (FilterData.Id.HasValue && FilterData.Id != Guid.Empty)
                query = query.Where(x => x.Id == FilterData.Id);
            if (!string.IsNullOrEmpty(FilterData.Username)) query = query.Where(x => x.Username == FilterData.Username);
            if (!string.IsNullOrEmpty(FilterData.Mail)) query = query.Where(x => x.Mail == FilterData.Mail);
            if (!string.IsNullOrEmpty(FilterData.Rank.ToString())) query = query.Where(x => x.Rank == FilterData.Rank);

            var result = await query.ProjectTo<vm_user>(mapper.ConfigurationProvider).FirstOrDefaultAsync();
            return result;
        }
        public async Task<List<vm_user>> GetUsers(rm_user_get? FilterData)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            // Return all users if FilterData is null or all its properties are empty/default
            if (FilterData == null ||
                (string.IsNullOrEmpty(FilterData.Username) &&
                 !FilterData.Rank.HasValue &&
                 string.IsNullOrEmpty(FilterData.Mail) &&
                 FilterData.Id == Guid.Empty))
            {
                return mapper.Map<List<vm_user>>(await _dbContext.Users.OrderByDescending(x => x.CreatedDate).ToListAsync());
            }

            var query = _dbContext.Users.AsQueryable();

            if (FilterData.Id.HasValue && FilterData.Id != Guid.Empty)
                query = query.Where(x => x.Id == FilterData.Id);

            if (!string.IsNullOrEmpty(FilterData.Username))
                query = query.Where(x => x.Username.Contains(FilterData.Username));

            if (!string.IsNullOrEmpty(FilterData.Mail))
                query = query.Where(x => x.Mail == FilterData.Mail);

            if (FilterData.Rank.HasValue)
                query = query.Where(x => x.Rank == FilterData.Rank);

            var result = await query
                .ProjectTo<vm_user>(mapper.ConfigurationProvider)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return result;
        }

        public async Task<List<vm_user_activity>> GetUserActivityList()
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Personel takibi için Admin yetkisi gereklidir.");

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var users = await _dbContext.Users
                .Where(u => u.Rank != Entity.Rank.Banned)
                .OrderByDescending(u => u.LastActivityDate)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            var officeNamesByUser = await _dbContext.User_Offices
                .Where(uo => userIds.Contains(uo.UserId) && uo.IsActive)
                .Select(uo => new { uo.UserId, OfficeName = uo.Office.OfficeName })
                .ToListAsync();

            var loginCounts = await _dbContext.UserLoginHistories
                .Where(h => userIds.Contains(h.UserId) && h.LoginDate >= monthStart)
                .GroupBy(h => h.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            return users.Select(u => new vm_user_activity
            {
                Id = u.Id,
                Firstname = u.Firstname,
                Lastname = u.Lastname,
                Username = u.Username,
                Rank = u.Rank.ToString(),
                Offices = officeNamesByUser.Where(o => o.UserId == u.Id).Select(o => o.OfficeName).ToList(),
                LastLoginDate = u.LastLoginDate,
                LastActivityDate = u.LastActivityDate,
                IsOnline = u.LastActivityDate.HasValue && (now - u.LastActivityDate.Value).TotalMinutes < 5,
                LoginCountThisMonth = loginCounts.FirstOrDefault(l => l.UserId == u.Id)?.Count ?? 0
            }).ToList();
        }

        public async Task<List<vm_user_login_history_entry>> GetUserLoginHistory(Guid userId, int? year, int? month)
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Personel takibi için Admin yetkisi gereklidir.");

            var query = _dbContext.UserLoginHistories.Where(h => h.UserId == userId);

            if (year.HasValue && month.HasValue)
            {
                var start = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                var end = start.AddMonths(1);
                query = query.Where(h => h.LoginDate >= start && h.LoginDate < end);
            }

            return await query
                .OrderByDescending(h => h.LoginDate)
                .Select(h => new vm_user_login_history_entry
                {
                    LoginDate = h.LoginDate,
                    IpAddress = h.IpAddress,
                    UserAgent = h.UserAgent
                })
                .ToListAsync();
        }

        public async Task<vm_user_daily_detail> GetUserDailyDetail(Guid userId, DateTime date)
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Personel takibi için Admin yetkisi gereklidir.");

            const int IdleGapMinutes = 15;

            // Türkiye saati (UTC+3, DST yok) — tarih parametresi yerel iş günü olarak yorumlanır.
            var dayStartUtc = date.Date.AddHours(-3);
            var dayEndUtc = dayStartUtc.AddDays(1);

            var pings = await _dbContext.UserActivityHistories
                .Where(h => h.UserId == userId && h.PingDate >= dayStartUtc && h.PingDate < dayEndUtc)
                .OrderBy(h => h.PingDate)
                .Select(h => h.PingDate)
                .ToListAsync();

            var sessions = new List<vm_user_session>();
            foreach (var ping in pings)
            {
                if (sessions.Count > 0 && (ping - sessions[sessions.Count - 1].End).TotalMinutes <= IdleGapMinutes)
                    sessions[sessions.Count - 1].End = ping;
                else
                    sessions.Add(new vm_user_session { Start = ping, End = ping });
            }

            var officeIds = await _dbContext.User_Offices
                .Where(uo => uo.UserId == userId && uo.IsActive)
                .Select(uo => uo.OfficeId)
                .ToListAsync();

            var dayClosures = await _dbContext.DayClosures
                .Where(dc => officeIds.Contains(dc.OfficeId) && dc.BusinessDate.Date == date.Date)
                .Select(dc => new vm_user_day_closure_status
                {
                    OfficeName = dc.Office.OfficeName,
                    Status = dc.Status.ToString(),
                    ClosedByUser = dc.ClosedByUser.Username,
                    ClosedAt = dc.ClosedAt
                })
                .ToListAsync();

            var transactions = await _dbContext.Transactions
                .Where(t => t.UserId == userId && t.CreatedDate >= dayStartUtc && t.CreatedDate < dayEndUtc)
                .Select(t => new { t.CreatedDate, t.TransactionDate })
                .ToListAsync();

            // Kişisel baseline: son 14 günün (bugün hariç), en az 5 işlemli günlerinin dağılım oranları
            var historyStartUtc = dayStartUtc.AddDays(-14);
            var historicalTransactions = await _dbContext.Transactions
                .Where(t => t.UserId == userId && t.CreatedDate >= historyStartUtc && t.CreatedDate < dayStartUtc)
                .Select(t => t.CreatedDate)
                .ToListAsync();

            var historicalDailyRatios = historicalTransactions
                .GroupBy(d => d.AddHours(3).Date)
                .Where(g => g.Count() >= 5)
                .Select(g => BulkEntryHeuristic.ComputeSpreadRatio(g.ToList()).Value)
                .ToList();

            var heuristic = BulkEntryHeuristic.Evaluate(
                transactions.Select(t => (t.CreatedDate, t.TransactionDate)).ToList(),
                historicalDailyRatios);

            return new vm_user_daily_detail
            {
                Sessions = sessions,
                DayClosures = dayClosures,
                TransactionCount = heuristic.TransactionCount,
                FirstTransactionAt = heuristic.FirstTransactionAt,
                LastTransactionAt = heuristic.LastTransactionAt,
                IsBulkEntrySuspected = heuristic.IsBulkEntrySuspected,
                BackdatedCount = heuristic.BackdatedCount,
                TodayRatio = heuristic.TodayRatio,
                BaselineRatio = heuristic.BaselineRatio
            };
        }

    }
}
