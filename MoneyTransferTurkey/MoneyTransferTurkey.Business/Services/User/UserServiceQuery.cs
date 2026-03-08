using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MoneyTransferTurkey.Business.Exceptions;
using MoneyTransferTurkey.Business.Infrastructure.User;
using MoneyTransferTurkey.Business.Services.Permission;
using MoneyTransferTurkey.Data.Contexts;
using MoneyTransferTurkey.Entity.Modals.RequestModals.User;
using MoneyTransferTurkey.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.User
{
    public class UserServiceQuery : IUserServiceQuery
    {
        private readonly MoneyTransferTurkeyDbContext _dbContext;
        private readonly IMapper mapper;
        private readonly ValidationService _validationService;

        public UserServiceQuery(MoneyTransferTurkeyDbContext dbContext, IMapper mapper, ValidationService validationService)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
            _validationService = validationService;
        }
  
        public async Task<vm_user> GetUser(rm_user_get FilterData)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

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

    }
}
