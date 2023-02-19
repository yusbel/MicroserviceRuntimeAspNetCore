using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sample.Messaging.Publishers;
using Sample.Messaging.WebHooks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging.Middleware
{
    public class MessageMiddleware
    {
        private readonly IMessagePublisher _msgPublisher;
        private RequestDelegate _next;
        private ILogger<MessageMiddleware> _logger;
        private IWebHookMessageSubscription _webHookMsgSubscriber;

        public MessageMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IWebHookMessageSubscription webHookMessageSubscriber, IMessagePublisher msgPublisher) =>
            (_next, _logger, _webHookMsgSubscriber, _msgPublisher) = (next, loggerFactory.CreateLogger<MessageMiddleware>(), webHookMessageSubscriber, msgPublisher);

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/WebHook/Message" && context.Request.Method == HttpMethod.Post.ToString())
            {
                _logger.LogInformation("============Web hook message request=============");
                context.Request.EnableBuffering();
                var messageKey = context.Request.Headers["MessageKey"];
                var readResult = await context.Request.BodyReader.ReadAsync();
                List<byte> bytes = new List<byte>();
                foreach (var item in readResult.Buffer)
                {
                    bytes.AddRange(item.ToArray());
                }
                var messageJsonString = Encoding.UTF8.GetString(bytes.ToArray());
                _logger.LogInformation($"Message received via post {messageJsonString}");
                _webHookMsgSubscriber.AddMessage(messageKey, messageJsonString);
                _logger.LogInformation("===================================================");
                _logger.LogInformation("========Publishing all message, I need to add more code for this===");
                await _msgPublisher.Publish();
                _logger.LogInformation("========Publishing completed=======================================");
                return;
            }
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Hello world from message middleware"));
        }
    }
}
