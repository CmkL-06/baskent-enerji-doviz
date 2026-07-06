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
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.Email;
using BaskentEnerji.Business.Infrastructure.User;
using BaskentEnerji.Business.Services.Email;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Business.Tools;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.User;
using BaskentEnerji.Entity.Modals.RequestModals.User;
using BaskentEnerji.Entity.Modals.ResponseModals.User;
using BaskentEnerji.Entity.Modals.ViewModals.User;

namespace BaskentEnerji.Business.Services.User
{
    public class UserServiceCommand : IUserServiceCommand
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecretKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpiryHours;
        private readonly IEmailSender _emailSender;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;

        public UserServiceCommand(BaskentEnerjiDbContext smileMedicalDbContext, IConfiguration configuration, IEmailSender emailSender, ValidationService validationService, IMapper mapper)
        {
            _dbContext = smileMedicalDbContext;
            _configuration = configuration;
            _jwtSecretKey = configuration["JwtSecretKey"];
            _jwtIssuer = configuration["JwtIssuer"];
            _jwtAudience = configuration["JwtAudience"];
            _jwtExpiryHours = int.TryParse(configuration["JwtExpiryHours"], out var h) ? h : 8;
            _emailSender = emailSender;
            _validationService = validationService;
            this.mapper = mapper;
        }

        public async Task<rsp_user_login> Authenticate(rm_user_login requestData, HttpContext httpContext)
        {
            if (requestData == null || string.IsNullOrWhiteSpace(requestData.Mail))
                throw new ApiException(HttpStatusCode.BadRequest, "Mail veya kullanıcı adı gerekli.");
            if (string.IsNullOrEmpty(requestData.Password))
                throw new ApiException(HttpStatusCode.BadRequest, "Şifre gerekli.");

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Mail == requestData.Mail || u.Username == requestData.Mail);

            if (user == null || !VerifyPassword(requestData.Password, user.Password))
                throw new ApiException(HttpStatusCode.Unauthorized, "User credentials are wrong");

            // Auto-migrate legacy SHA-256 hash to BCrypt on successful login
            if (!user.Password.StartsWith("$2"))
            {
                user.Password = HashPassword(requestData.Password);
                await _dbContext.SaveChangesAsync();
            }

            if (user.Rank == Entity.Rank.Banned)
                throw new ApiException(HttpStatusCode.Forbidden, "This account has been suspended.");

            // Generate a Firebase token for the user
            var uid = user.Id.ToString();  // Use user's unique ID as Firebase UID
          //  var firebaseToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid);

            // Optionally, also generate a JWT token
            var jwtTokenString = GenerateJwtToken(user);

            user.LastIp = tools_string.GetIpAddress(httpContext);

            var userOffice = await _dbContext.User_Offices
                .Where(uo => uo.UserId == user.Id && uo.IsActive)
                .Select(uo => uo.OfficeId)
                .FirstOrDefaultAsync();

            return new rsp_user_login
            {
                ApiToken = jwtTokenString,
                UserInfo = new vm_user_simple
                {
                    Id = user.Id,
                    Mail = user.Mail ?? "",
                    FullName = $"{user.Firstname ?? ""} {user.Lastname ?? ""}".Trim(),
                    CreatedDate = user.CreatedDate,
                    Firstname = user.Firstname ?? "",
                    Lastname = user.Lastname ?? "",
                    Rank = user.Rank,
                    Username = user.Username ?? "",
                    OfficeId = userOffice != Guid.Empty ? userOffice : null
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
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            // Legacy SHA-256 (no $2 prefix): migrate transparently on next login
            if (!storedHash.StartsWith("$2"))
            {
                using var sha256 = SHA256.Create();
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString() == storedHash;
            }
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        private string GenerateJwtToken(Entity.Entities.User.User user)
        {
            if (string.IsNullOrEmpty(_jwtSecretKey))
                throw new InvalidOperationException("JWT yapilandirmasi eksik (JwtSecretKey). appsettings.json kontrol edin.");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Mail ?? ""),
                    new Claim(ClaimTypes.Role, user.Rank.ToString()),
                    new Claim("Rank", ((int)user.Rank).ToString()),
                    new Claim(ClaimTypes.Name, user.Username ?? ""),
                }),
                Expires = DateTime.UtcNow.AddHours(_jwtExpiryHours),
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
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
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
            var isOwner = await _validationService.IsOwnerAsync();
            var isAdmin = await _validationService.IsAdminAsync();

            if (!isAdmin)
            {
                userData.Id  = dbUser.Id;
                userData.Mail = dbUser.Mail;
                userData.Username = dbUser.Username;
                userData.Rank = dbUser.Rank;
                userData.IsEmailVerified = dbUser.IsEmailVerified;
            }
            else if (isAdmin && !isOwner)
            {
                userData.Rank = dbUser.Rank;
            }

            _dbContext.Entry(dbUser).CurrentValues.SetValues(userData);
            await _dbContext.SaveChangesAsync();

        }

        public async Task<bool> ChangeUserPassword(rm_change_user_password requestData)
        {
            if (!await _validationService.IsOwnerAsync() && !await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Bu işlem için Admin veya Owner yetkisi gereklidir.");

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
            if (!await _validationService.IsOwnerAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Bu işlem için Owner yetkisi gereklidir.");

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
