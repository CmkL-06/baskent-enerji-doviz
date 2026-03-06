using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmileMedical.Business.Exceptions;
using SmileMedical.Business.Infrastructure.Email;
using SmileMedical.Business.Infrastructure.User;
using SmileMedical.Business.Services.Email;
using SmileMedical.Business.Services.Permission;
using SmileMedical.Business.Tools;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.User;
using SmileMedical.Entity.Modals.RequestModals.User;
using SmileMedical.Entity.Modals.ResponseModals.User;
using SmileMedical.Entity.Modals.ViewModals.User;

namespace SmileMedical.Business.Services.User
{
    public class UserServiceCommand : IUserServiceCommand
    {
        private readonly SmileMedicalDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecretKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly IEmailSender _emailSender;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;

        public UserServiceCommand(SmileMedicalDbContext smileMedicalDbContext, IConfiguration configuration, IEmailSender emailSender, ValidationService validationService, IMapper mapper)
        {
            _dbContext = smileMedicalDbContext;
            _configuration = configuration;
            _jwtSecretKey = configuration["JwtSecretKey"];
            _jwtIssuer = configuration["JwtIssuer"];
            _jwtAudience = configuration["JwtAudience"];
            _emailSender = emailSender;
            _validationService = validationService;
            this.mapper = mapper;
        }

        public async Task<rsp_user_login> Authenticate(rm_user_login requestData, HttpContext httpContext)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => (u.Mail == requestData.Mail || u.Username == requestData.Mail) && u.Password == HashPassword(requestData.Password));

            if (user == null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "User credentials are wrong");
            }

            if (user.Rank == Entity.Rank.Banned)
                throw new ApiException(HttpStatusCode.Forbidden, "This account has been suspended.");

            // Generate a Firebase token for the user
            var uid = user.Id.ToString();  // Use user's unique ID as Firebase UID
          //  var firebaseToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid);

            // Optionally, also generate a JWT token
            var jwtTokenString = GenerateJwtToken(user);

            user.LastIp = tools_string.GetIpAddress(httpContext);

            return new rsp_user_login
            {
                ApiToken = jwtTokenString,
                //FirebaseToken = firebaseToken,  // Add Firebase token here
                UserInfo = new vm_user_simple
                {
                    Id = user.Id,
                    Mail = user.Mail,
                    FullName = $"{user.Firstname} {user.Lastname}",
                    CreatedDate = user.CreatedDate,
                    Firstname = user.Firstname,
                    Lastname = user.Lastname,
                    Gender = user.Gender,
                    Rank = user.Rank,
                    Username = user.Username
                }
            };
        }

        public async Task<rsp_user_login> NewUser(rm_user_register requestData, HttpContext httpContext)
        {
            // Check if user already exists
            if (await _dbContext.Users.AnyAsync(u => u.Mail == requestData.Mail || u.Username == requestData.Username))
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Hash the password
            var passwordHash = HashPassword(requestData.Password);

            // Create and save the new user
            var user = new Entity.Entities.User.User
            {
                Id = Guid.NewGuid(),
                Username = requestData.Username,
                Mail = requestData.Mail,
                Password = passwordHash,
                Firstname = requestData.Firstname,
                Lastname = requestData.Lastname,
                Gender = requestData.Gender,
                IsEmailVerified = false,
                Rank = Entity.Rank.User,
                FirstIp = tools_string.GetIpAddress(httpContext),
                LastIp = tools_string.GetIpAddress(httpContext),
                
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Send email activation token
            var activationToken = GenerateEmailToken(user);
            await SendActivationEmail(user, activationToken);

            // Generate JWT token
            var tokenString = GenerateJwtToken(user);

            return new rsp_user_login
            {
                ApiToken = tokenString,
                UserInfo = new vm_user_simple
                {
                    Id = user.Id,
                    Mail = user.Mail,
                    FullName = $"{user.Firstname} {user.Lastname}",
                    Username = user.Username,
                    Firstname = user.Firstname,
                    Lastname = user.Lastname,
                    Rank = user.Rank,
                    Gender = user.Gender
                }
            };
        }

        public async Task<string> GeneratePasswordResetToken(string email)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Mail == email);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            var token = GenerateEmailToken(user);
            await SendPasswordResetEmail(user, token);
            return token;
        }

        public async Task<bool> ResetPassword(string email, string token, string newPassword)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Mail == email);
            if (user == null || !ValidateToken(token, user))
            {
                return false; // Invalid token or user not found
            }

            user.Password = HashPassword(newPassword);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateEmail(string email, string token)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Mail == email);
            if (user == null || !ValidateToken(token, user))
            {
                return false; // Invalid token or user not found
            }

            user.IsEmailVerified = true;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private string GenerateJwtToken(Entity.Entities.User.User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("UserId", user.Id.ToString()), // Custom claim for middleware
                    new Claim(ClaimTypes.Email, user.Mail),
                    // new Claim(ClaimTypes.Role, user.Rank)
                }),
                Expires = DateTime.UtcNow.AddMonths(6),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateEmailToken(Entity.Entities.User.User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Mail)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private bool ValidateToken(string token, Entity.Entities.User.User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecretKey);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _jwtIssuer,
                    ValidAudience = _jwtAudience,
                    ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

                return userId == user.Id.ToString();
            }
            catch
            {
                return false;
            }
        }

        private async Task SendPasswordResetEmail(Entity.Entities.User.User user, string token)
        {
            //    var resetLink = $"https://yourdomain.com/reset-password?token={token}";
            var resetLink = $"{_configuration["site:domain"]}?token={token}";
            var subject = "Password Reset Request";
            var htmlMessage = $"<p>Please reset your password by clicking <a href='{resetLink}'>here</a>.</p>";
            await _emailSender.SendEmailAsync(user.Mail, subject, htmlMessage);
        }

        private async Task SendActivationEmail(Entity.Entities.User.User user, string token)
        {
            var activationLink = $"{_configuration["site:domain"]}/user/activate-mail?token={token}&email={user.Mail}";
            var subject = "Email Activation";
            var htmlMessage = $"<p>Please activate your email by clicking <a href='{activationLink}'>here</a>.</p>";
            await _emailSender.SendEmailAsync(user.Mail, subject, htmlMessage);
        }

        public async Task UpdateUser(rm_user_update userData)
        {
            if (!await _validationService.HasPermissionAsync(userData.Id))
                throw new ApiException(HttpStatusCode.NotFound, "You have no permission to do this.");
            if (userData == null)
                throw new ApiException(HttpStatusCode.NotFound, "User couldn't be found.");


            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userData.Id);
            if (!await _validationService.IsAdminAsync())
            {

                userData.Id  = dbUser.Id;
                userData.Mail = dbUser.Mail;
                userData.Username = dbUser.Username;
                userData.Rank = dbUser.Rank;
              
            }
            _dbContext.Entry(dbUser).CurrentValues.SetValues(userData);
            await _dbContext.SaveChangesAsync();

        }

        public async Task<bool> ChangeUserPassword(rm_change_user_password requestData)
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Only admins can change user passwords.");

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == requestData.UserId);
            if (user == null)
                throw new ApiException(HttpStatusCode.NotFound, "User not found.");

            // Hash the new password
            user.Password = HashPassword(requestData.NewPassword);

            // Update last password change date to invalidate existing tokens
            user.LastPasswordChangeDate = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LogoutAllUsers()
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Only admins can logout all users.");

            var allUsers = await _dbContext.Users.ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var user in allUsers)
            {
                user.LastPasswordChangeDate = now;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
