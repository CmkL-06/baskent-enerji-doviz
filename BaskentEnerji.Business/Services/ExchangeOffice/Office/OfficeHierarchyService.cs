using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class OfficeHierarchyService : IOfficeHierarchyService
    {
        private readonly BaskentEnerjiDbContext _db;

        public OfficeHierarchyService(BaskentEnerjiDbContext db)
        {
            _db = db;
        }

        public async Task<List<vm_office>> GetHierarchyAsync()
        {
            var all = await _db.Offices
                .Include(o => o.Vaults)
                .Include(o => o.Employees)
                .Where(o => o.IsActive)
                .OrderBy(o => o.OfficeType)
                .ThenBy(o => o.OfficeName)
                .ToListAsync();

            // Merkez ofisleri root olarak al, altına child'ları ekle
            var roots = all
                .Where(o => o.ParentOfficeId == null)
                .Select(o => MapToVm(o, all))
                .ToList();

            return roots;
        }

        public async Task<vm_office?> GetMerkezAsync()
        {
            var merkez = await _db.Offices
                .Include(o => o.Vaults)
                .Include(o => o.Employees)
                .FirstOrDefaultAsync(o => o.OfficeType == OfficeType.Merkez && o.IsActive);

            return merkez == null ? null : MapToVm(merkez, null);
        }

        public async Task<List<vm_office>> GetChildrenAsync(Guid parentOfficeId)
        {
            var children = await _db.Offices
                .Include(o => o.Vaults)
                .Include(o => o.Employees)
                .Where(o => o.ParentOfficeId == parentOfficeId && o.IsActive)
                .OrderBy(o => o.OfficeName)
                .ToListAsync();

            return children.Select(o => MapToVm(o, null)).ToList();
        }

        public async Task<List<vm_useroffice>> GetUserAccessibleOfficesAsync(Guid userId)
        {
            var userOffices = await _db.User_Offices
                .Include(uo => uo.Office).ThenInclude(o => o.Vaults)
                .Include(uo => uo.User)
                .Where(uo => uo.UserId == userId && uo.IsActive && uo.Office.IsActive)
                .ToListAsync();

            return userOffices.Select(uo => new vm_useroffice
            {
                Id = uo.Id,
                UserId = uo.UserId,
                OfficeId = uo.OfficeId,
                Role = uo.Role.ToString(),
                IsActive = uo.IsActive,
                CreatedDate = uo.CreatedDate,
                Office = MapToVm(uo.Office, null)
            }).ToList();
        }

        private static vm_office MapToVm(Entity.Entities.ExchangeOffice.Office.Office o,
            List<Entity.Entities.ExchangeOffice.Office.Office>? all) => new()
        {
            Id = o.Id,
            CreatedDate = o.CreatedDate,
            OfficeName = o.OfficeName,
            OfficeDescription = o.OfficeDescription,
            OfficeImageUri = o.OfficeImageUri,
            Address = o.Address,
            Phone = o.Phone,
            IsActive = o.IsActive,
            OfficeType = o.OfficeType.ToString(),
            ParentOfficeId = o.ParentOfficeId,
            DailyTransactionLimit = o.DailyTransactionLimit,
            MonthlyTransactionLimit = o.MonthlyTransactionLimit,
            CommissionRate = o.CommissionRate,
            VaultCount = o.Vaults?.Count ?? 0,
            UserCount = o.Employees?.Count ?? 0,
            Children = all == null ? null : all
                .Where(c => c.ParentOfficeId == o.Id)
                .Select(c => MapToVm(c, all))
                .ToList()
        };
    }
}
