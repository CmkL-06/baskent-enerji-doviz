using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BaskentEnerji.Business.Services.Permission;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.Telegram
{
    [Route("api/v1/tg")]
    [ApiController]
    public class TelegramEventsController : ControllerBase
    {
        private readonly ValidationService _validationService;
        private readonly IConfiguration _configuration;

        private static readonly ConcurrentDictionary<string, SseClient> _clients = new();

        public TelegramEventsController(ValidationService validationService, IConfiguration configuration)
        {
            _validationService = validationService;
            _configuration = configuration;
        }

        [HttpGet("events")]
        [Authorize]
        public async Task Events(CancellationToken ct)
        {
            if (!await _validationService.IsStaff())
            {
                HttpContext.Response.StatusCode = 403;
                return;
            }

            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            var clientId = Guid.NewGuid().ToString();
            var client = new SseClient();
            _clients.TryAdd(clientId, client);

            try
            {
                await Response.WriteAsync("data: connected\n\n", ct);
                await Response.Body.FlushAsync(ct);

                while (!ct.IsCancellationRequested)
                {
                    string? message = null;
                    try
                    {
                        message = await client.WaitForMessage(TimeSpan.FromSeconds(30), ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (message != null)
                        await Response.WriteAsync(message, ct);
                    else
                        await Response.WriteAsync(": keepalive\n\n", ct);

                    await Response.Body.FlushAsync(ct);
                }
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
            }
        }

        [HttpPost("notify")]
        [AllowAnonymous]
        public IActionResult Notify([FromBody] JsonElement data)
        {
            var secret = _configuration["NotifySecret"] ?? "bsk-notify-2026-secret";
            var headerSecret = Request.Headers["X-Notify-Secret"].FirstOrDefault();
            if (headerSecret != secret)
                return Unauthorized(new { ok = false, error = "Invalid secret" });

            var message = $"event: transaction_update\ndata: {data.GetRawText()}\n\n";
            foreach (var client in _clients.Values)
                client.Send(message);
            return Ok(new { ok = true });
        }

        public static void Broadcast(string eventType, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var message = $"event: {eventType}\ndata: {json}\n\n";
            foreach (var client in _clients.Values)
                client.Send(message);
        }

        private class SseClient
        {
            private readonly SemaphoreSlim _signal = new(0);
            private readonly ConcurrentQueue<string> _queue = new();

            public void Send(string message)
            {
                _queue.Enqueue(message);
                _signal.Release();
            }

            public async Task<string?> WaitForMessage(TimeSpan timeout, CancellationToken ct)
            {
                if (await _signal.WaitAsync(timeout, ct))
                {
                    _queue.TryDequeue(out var msg);
                    return msg;
                }
                return null;
            }
        }
    }
}
