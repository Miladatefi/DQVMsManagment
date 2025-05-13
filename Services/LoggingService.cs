using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace DQVMsManagement.Services
{
    public class LoggingService
    {
        private readonly string _logPath;
        private readonly string _historyPath = @"C:\loginHistory.json";
        private readonly IHttpContextAccessor _http;

        public LoggingService(IHttpContextAccessor httpContextAccessor)
        {
            _http = httpContextAccessor;
            Directory.CreateDirectory(@"C:\logs");
            _logPath = Path.Combine(@"C:\logs", "app.log");

            if (!File.Exists(_historyPath))
                File.WriteAllText(_historyPath, "[]");
        }

        public async Task LogAsync(string message, string? overrideUser = null)
        {
            var now  = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var ctx  = _http.HttpContext;
            string ip = "unknown-ip";

            if (ctx is not null)
            {
                var header = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(header))
                    ip = header.Split(',').First().Trim();
                else
                    ip = ctx.Connection.RemoteIpAddress?.ToString() ?? ip;
            }

            // Use overrideUser if provided, else use current user identity
            var user = overrideUser ?? ctx?.User?.Identity?.Name ?? "anonymous";
            var line = $"[{now}] [{ip}] [{user}] {message}";
            await File.AppendAllTextAsync(_logPath, line + Environment.NewLine);

            // Also record login events into JSON history
            if (message.Contains("logged in", StringComparison.OrdinalIgnoreCase))
            {
                var json = File.ReadAllText(_historyPath);
                var history = JsonConvert.DeserializeObject<List<LoginRecord>>(json)
                              ?? new List<LoginRecord>();

                history.Add(new LoginRecord
                {
                    Username  = user,
                    Ip        = ip,
                    Timestamp = DateTime.Parse(now)
                });

                File.WriteAllText(_historyPath, JsonConvert.SerializeObject(history, Formatting.Indented));
            }
        }
    }

    public class LoginRecord
    {
        public string   Username  { get; set; } = string.Empty;
        public string   Ip        { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}