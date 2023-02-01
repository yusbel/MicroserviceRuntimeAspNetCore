using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Employee.WebHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Employee.Middleware
{
    public class WebHookMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebHookMiddleware> _logger;

        public WebHookMiddleware(RequestDelegate next, ILoggerFactory loggerFactory) => 
            (_next, _logger) = (next, loggerFactory.CreateLogger<WebHookMiddleware>());

        public async Task InvokeAsync(HttpContext context) 
        {
            if (context.Request.Path == "/WebHook") 
            {
                _logger.LogInformation($"======Processing a web hook request======");
                context.Request.EnableBuffering();
                var segments = await context.Request.BodyReader.ReadAsync();
                var msgBytes = new List<byte>();
                foreach (var segment in segments.Buffer) 
                {
                    msgBytes.AddRange(segment.ToArray());
                }
                var payRoll = System.Text.Json.JsonSerializer.Deserialize<PayRollData>(Encoding.UTF8.GetString(msgBytes.ToArray()));
                _logger.LogInformation($"PayRoll: {System.Text.Json.JsonSerializer.Serialize(payRoll)}");
                _logger.LogInformation("============================================");
                return;
            }
            await _next(context);
        }
    }
}
