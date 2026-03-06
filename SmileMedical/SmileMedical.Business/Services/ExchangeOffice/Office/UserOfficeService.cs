using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmileMedical.Business.Infrastructure.ExchangeOffice.Office;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.ExchangeOffice.Office;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.ExchangeOffice.Office
{
    public class UserOfficeService : IUserOfficeService
    {
        private readonly SmileMedicalDbContext _context;
        private readonly IMapper _mapper;

        public UserOfficeService(SmileMedicalDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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

        public async Task<List<SmileMedical.Entity.Modals.ViewModals.User.vm_user>> GetUsersByOfficeAsync(Guid officeId)
        {
            var users = await _context.User_Offices
                .Include(uo => uo.User)
                .Where(uo => uo.OfficeId == officeId)
                .Select(uo => uo.User)
                .ToListAsync();

            return _mapper.Map<List<SmileMedical.Entity.Modals.ViewModals.User.vm_user>>(users);
        }
    }
}