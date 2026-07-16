using System;
using System.Security.Claims;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using Microsoft.AspNetCore.Http;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// ValidationService, giriş yapmış kullanıcıyı IHttpContextAccessor üzerindeki
    /// ClaimTypes.NameIdentifier claim'inden okuyor. Testlerde gerçek bir HTTP isteği
    /// olmadığı için, sahte bir HttpContext + claim ile ValidationService oluşturuyoruz.
    /// </summary>
    public static class TestAuthHelper
    {
        public static ValidationService CreateValidationService(Guid userId, BaskentEnerjiDbContext context)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"));

            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            return new ValidationService(accessor, context);
        }
    }
}
