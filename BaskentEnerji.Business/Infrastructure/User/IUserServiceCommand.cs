using Microsoft.AspNetCore.Http;
using BaskentEnerji.Entity.Entities.User;
using BaskentEnerji.Entity.Modals.RequestModals.User;
using BaskentEnerji.Entity.Modals.ResponseModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.User
{
    public interface IUserServiceCommand
    {
        Task<rsp_user_login> Authenticate(rm_user_login requestData, HttpContext httpContext);
        Task <rsp_user_login> NewUser (rm_user_register requestData, HttpContext httpContext);
        Task<string> GeneratePasswordResetToken(string email);
        Task<bool> ResetPassword(string email, string token, string newPassword);
        Task<bool> ActivateEmail(string email, string token);
        Task UpdateUser(rm_user_update userData);
        Task<bool> ChangeUserPassword(rm_change_user_password requestData);
        Task<bool> ChangeOwnPassword(Guid userId, string currentPassword, string newPassword);
        Task<bool> LogoutAllUsers();
    }
}
