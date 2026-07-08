using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.User;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Entity.Modals.RequestModals.User;
using BaskentEnerji.Entity.Modals.ResponseModals.User;
using BaskentEnerji.Entity.Modals.ViewModals.User;
using System.Net;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserServiceCommand _userServiceCommand;
        private readonly IUserServiceQuery _userServiceQuery;
        private readonly ValidationService _validationService;

        public UserController(IUserServiceCommand userServiceCommand, IUserServiceQuery userServiceQuery, ValidationService validationService)
        {
            _userServiceCommand = userServiceCommand;
            _userServiceQuery = userServiceQuery;
            _validationService = validationService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<rsp_user_login> Login(rm_user_login requestData)
        {
            return await _userServiceCommand.Authenticate(requestData, HttpContext);
        }

        [HttpPost("register")]
        [Authorize]
        public async Task<rsp_user_login> Register(rm_user_register requestData)
        {
            if (!await _validationService.IsOwnerAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Kullanıcı ekleme için Owner yetkisi gereklidir.");

            return await _userServiceCommand.NewUser(requestData, HttpContext);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> ForgotPassword([FromBody] rm_forgot_password requestData)
        {
            await _userServiceCommand.GeneratePasswordResetToken(requestData.Email);
            return Ok(new { Message = "If this email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> ResetPassword([FromBody] rm_reset_password requestData)
        {
            var result = await _userServiceCommand.ResetPassword(requestData.Email, requestData.Token, requestData.NewPassword);
            if (result)
            {
                return Ok(new { Message = "Password reset successfully" });
            }
            return BadRequest(new { Message = "Invalid token or email" });
        }

        [HttpPost("activate-email")]
        [Authorize]
        public async Task<IActionResult> ActivateEmail([FromBody] rm_activate_email requestData)
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(System.Net.HttpStatusCode.Forbidden, "Bu işlem için Admin yetkisi gereklidir.");
            var result = await _userServiceCommand.ActivateEmail(requestData.Email, requestData.Token);
            if (result)
                return Ok(new { Message = "Email activated successfully" });
            return BadRequest(new { Message = "Invalid token or email" });
        }

        [HttpPost("update")]
        [Authorize]
        public async Task UpdateUser(rm_user_update userData)
        {
            if (!await _validationService.HasPermissionAsync(userData.Id))
                throw new ApiException(HttpStatusCode.Forbidden, "You have no permission to do this.");

            await _userServiceCommand.UpdateUser(userData);
        }

        [HttpGet("Users")]
        [Authorize]
        public async Task<List<vm_user>> GetUsers ([FromQuery] rm_user_get? FilterData)
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Kullanıcı listesi için Admin yetkisi gereklidir.");

            return await _userServiceQuery.GetUsers(FilterData);
        }

        [HttpGet("User")]
        [Authorize]
        public async Task<vm_user> GetUser ([FromQuery] rm_user_get FilterData)
        {
            return await _userServiceQuery.GetUser(FilterData);
        }

        [HttpPost("change-password")]
        [Authorize]
        [EnableRateLimiting("sensitive")]
        public async Task<IActionResult> ChangeUserPassword([FromBody] rm_change_user_password requestData)
        {
            var result = await _userServiceCommand.ChangeUserPassword(requestData);
            if (result)
            {
                return Ok(new {
                    Message = "Password changed successfully",
                    UserId = requestData.UserId.ToString(),
                    ShouldLogout = true
                });
            }
            return BadRequest(new { Message = "Failed to change password" });
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAllUsers()
        {
            var result = await _userServiceCommand.LogoutAllUsers();
            if (result)
            {
                return Ok(new {
                    Message = "All users have been logged out successfully"
                });
            }
            return BadRequest(new { Message = "Failed to logout all users" });
        }

    }
}
