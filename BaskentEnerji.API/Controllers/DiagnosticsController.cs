using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Data.Contexts;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BaskentEnerji.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class DiagnosticsController : ControllerBase
    {
        private readonly BaskentEnerjiDbContext _db;

        public DiagnosticsController(BaskentEnerjiDbContext db)
        {
            _db = db;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                Ok = true,
                Service = "BaskentEnerji.API",
                Utc = DateTime.UtcNow
            });
        }

        [HttpGet("version")]
        public IActionResult Version()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            return Ok(new
            {
                Ok = true,
                Service = assemblyName.Name,
                Version = assemblyName.Version?.ToString(),
                InformationalVersion = informationalVersion,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Framework = RuntimeInformation.FrameworkDescription,
                Commit = Environment.GetEnvironmentVariable("GIT_COMMIT_SHA")
            });
        }

        [HttpGet("system")]
        public IActionResult SystemInfo()
        {
            using var process = Process.GetCurrentProcess();
            var startedUtc = process.StartTime.ToUniversalTime();
            var uptime = DateTime.UtcNow - startedUtc;

            return Ok(new
            {
                Ok = true,
                Machine = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                StartedUtc = startedUtc,
                UptimeSeconds = (long)uptime.TotalSeconds,
                UtcNow = DateTime.UtcNow
            });
        }

        [HttpGet("db")]
        public async Task<IActionResult> TestDb()
        {
            var startedAt = DateTime.UtcNow;
            try
            {
                var canConnect = await _db.Database.CanConnectAsync();
                var userCount = await _db.Users.CountAsync();
                return Ok(new
                {
                    Ok = true,
                    CanConnect = canConnect,
                    UserCount = userCount,
                    Provider = _db.Database.ProviderName,
                    DurationMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds
                });
            }
            catch (Exception ex)
            {
                var msg = (ex.InnerException?.Message ?? ex.Message) ?? "?";
                return Ok(new
                {
                    Ok = false,
                    Error = msg,
                    Provider = _db.Database.ProviderName,
                    DurationMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds
                });
            }
        }
    }
}
