using AutoMapper;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class UserOfficeService : IUserOfficeService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IMapper _mapper;
        private readonly ValidationService _validationService;

        public UserOfficeService(BaskentEnerjiDbContext context, IMapper mapper, ValidationService validationService)
        {
            _context = context;
            _mapper = mapper;
            _validationService = validationService;
        }

        // Bir kullanıcının hangi ofislere bağlı olduğunu değiştirmek (dolayısıyla o ofislerdeki
        // rolünü/erişimini belirlemek) tamamen ofis-bağımsız, sistem geneli bir yetki devridir —
        // bu yüzden tekil bir officeId'ye göre değil, doğrudan Admin/Owner rütbesine göre korunur.
        // Aksi halde herhangi bir kimliği doğrulanmış kullanıcı kendini istediği ofise ekleyebilir.
        private async Task EnsureAdminAsync()
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Bu işlem için Admin veya Owner yetkisi gerekir.");
        }

        public async Task<List<vm_useroffice>> GetUserOfficesAsync(Guid userId)
        {
            var userOffices = await _context.User_Offices
                .Include(uo => uo.Office)
                .Include(uo => uo.User)
                .Where(uo => uo.UserId == userId)
                .ToListAsync();

            return _mapper.Map<List<vm_useroffice>>(userOffices);
        }

        public async Task<vm_useroffice> GetUserOfficeAsync(Guid userId, Guid officeId)
        {
            var userOffice = await _context.User_Offices
                .Include(uo => uo.Office)
                .Include(uo => uo.User)
                .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.OfficeId == officeId);

            return _mapper.Map<vm_useroffice>(userOffice);
        }

        public async Task<List<vm_office>> GetOfficesByUserAsync(Guid userId)
        {
            var offices = await _context.User_Offices
                .Include(uo => uo.Office)
                .Where(uo => uo.UserId == userId && uo.Office.IsActive)
                .Select(uo => uo.Office)
                .ToListAsync();

            return _mapper.Map<List<vm_office>>(offices);
        }

        public async Task<vm_useroffice> AttachOfficeToUserAsync(rm_useroffice model)
        {
            await EnsureAdminAsync();
            try
            {
                var existingUserOffice = await _context.User_Offices
                    .FirstOrDefaultAsync(uo => uo.UserId == model.UserId && uo.OfficeId == model.OfficeId);

                if (existingUserOffice != null)
                {
                    return _mapper.Map<vm_useroffice>(existingUserOffice);
                }

                var userOffice = new User_Office
                {
                    Id = Guid.NewGuid(),
                    UserId = model.UserId,
                    OfficeId = model.OfficeId,
                    CreatedDate = DateTime.Now
                };

                await _context.User_Offices.AddAsync(userOffice);
                await _context.SaveChangesAsync();

                return await GetUserOfficeAsync(model.UserId, model.OfficeId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error attaching office to user: {ex.Message}", ex);
            }
        }

        public async Task<bool> RemoveOfficeFromUserAsync(Guid userId, Guid officeId)
        {
            await EnsureAdminAsync();
            try
            {
                var userOffice = await _context.User_Offices
                    .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.OfficeId == officeId);

                if (userOffice == null)
                    return false;

                _context.User_Offices.Remove(userOffice);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error removing office from user: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateUserOfficesAsync(Guid userId, List<Guid> officeIds)
        {
            await EnsureAdminAsync();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUserOffices = await _context.User_Offices
                    .Where(uo => uo.UserId == userId)
                    .ToListAsync();

                var officesToRemove = existingUserOffices
                    .Where(uo => !officeIds.Contains(uo.OfficeId))
                    .ToList();
                
                _context.User_Offices.RemoveRange(officesToRemove);

                var existingOfficeIds = existingUserOffices.Select(uo => uo.OfficeId).ToList();
                var newOfficeIds = officeIds.Where(id => !existingOfficeIds.Contains(id)).ToList();

                foreach (var officeId in newOfficeIds)
                {
                    var userOffice = new User_Office
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        OfficeId = officeId,
                        CreatedDate = DateTime.Now
                    };
                    await _context.User_Offices.AddAsync(userOffice);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Error updating user offices: {ex.Message}", ex);
            }
        }

        public async Task<bool> HasUserAccessToOfficeAsync(Guid userId, Guid officeId)
        {
            return await _context.User_Offices
                .AnyAsync(uo => uo.UserId == userId && uo.OfficeId == officeId);
        }

        public async Task<List<BaskentEnerji.Entity.Modals.ViewModals.User.vm_user>> GetUsersByOfficeAsync(Guid officeId)
        {
            var users = await _context.User_Offices
                .Include(uo => uo.User)
                .Where(uo => uo.OfficeId == officeId)
                .Select(uo => uo.User)
                .ToListAsync();

            return _mapper.Map<List<BaskentEnerji.Entity.Modals.ViewModals.User.vm_user>>(users);
        }
    }
}