using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MoneyTransfer.API.Models;

namespace MoneyTransfer.API.Helpers;

public static class JwtHelper
{
    public static string GenerateToken(User user, string secret, int expiryDays = 7)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name,           user.Username),
            new(ClaimTypes.Role,           user.Role),
            new("name",                    user.Name)
        };

        // Admin için ekstra claim
        if (user.Role == "operator" && user.Operator?.IsAdmin == true)
            claims.Add(new("is_admin", "true"));

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: creds,
            claims: claims
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateCode(int length = 8)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
