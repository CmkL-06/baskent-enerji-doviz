using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransferTurkey.Business.Exceptions;
using MoneyTransferTurkey.Business.Infrastructure.User;
using MoneyTransferTurkey.Business.Services.Permission;
using MoneyTransferTurkey.Entity.Modals.RequestModals.User;
using MoneyTransferTurkey.Entity.Modals.ResponseModals.User;
using MoneyTransferTurkey.Entity.Modals.ViewModals.User;
using System.Net;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.API.Controllers
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
        public async Task<rsp_user_login> Login(rm_user_login requestData)
        {
            return await _userServiceCommand.Authenticate(requestData, HttpContext);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<rsp_user_login> Register(rm_user_register requestData)
        {
            try
            {
                return await _userServiceCommand.NewUser(requestData,HttpContext);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] rm_forgot_password requestData)
        {
            var token = await _userServiceCommand.GeneratePasswordResetToken(requestData.Email);
            return Ok(new { Token = token });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
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
        [AllowAnonymous]
        public async Task<IActionResult> ActivateEmail([FromBody] rm_activate_email requestData)
        {
            var result = await _userServiceCommand.ActivateEmail(requestData.Email, requestData.Token);
            if (result)
            {
                return Ok(new { Message = "Email activated successfully" });
            }
            return BadRequest(new { Message = "Invalid token or email" });
        }

        [HttpGet("activate-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ActivateEmail([FromQuery] string token, [FromQuery] string email)
        {
            var result = await _userServiceCommand.ActivateEmail(email, token);
            if (result)
            {
                return Ok(new { Message = "Email activated successfully" });
            }
            return BadRequest(new { Message = "Invalid token or email" });
        }

        [HttpPost("update")]
        public async Task UpdateUser(rm_user_update userData)
        {
            if (!await _validationService.HasPermissionAsync(userData.Id))
                throw new ApiException(HttpStatusCode.NoContent, "You have no permission to do this.");

            await _userServiceCommand.UpdateUser(userData);
        }

        [HttpGet("Users")]
        public async Task<List<vm_user>> GetUsers ([FromQuery] rm_user_get? FilterData)
        {
            return await _userServiceQuery.GetUsers(FilterData);
        }

        [HttpGet("User")]
        [AllowAnonymous]
        public async Task<vm_user> GetUser ([FromQuery] rm_user_get FilterData)
        {
            return await _userServiceQuery.GetUser(FilterData);
        }

        [HttpPost("change-password")]
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
