using Microsoft.AspNetCore.Http;
using Sample.Messaging.WebHooks;
using Sample.Messaging.WebHooks.Data;
using Sample.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging.Middleware
{
    /// <summary>
    /// Process request send to /WebHook to create webhook subscritions and retrieve them using subscriber key
    /// </summary>
    public class WebHookMiddleware
    {
        private RequestDelegate _next;
        private IWebHookSubscription _webHookSubscribers;

        public WebHookMiddleware(RequestDelegate next, IWebHookSubscription webHookSubscribers) => (_next, _webHookSubscribers) = (next, webHookSubscribers);
        public async Task InvokeAsync(HttpContext context)
        {
            Guard.ThrowWhenNull(context);
            if (context.Request.Path != "/WebHook")
            {
                await _next(context);
                return;
            }
            if (context.Request.Method == HttpMethod.Post.ToString())
            {
                context.Request.EnableBuffering();
                var segments = await context.Request.BodyReader.ReadAsync();
                List<byte> msg = new List<byte>();
                foreach (var segment in segments.Buffer)
                {
                    msg.AddRange(segment.ToArray());
                }
                var jsonString = Encoding.UTF8.GetString(msg.ToArray());
                var webHookSubscriber = System.Text.Json.JsonSerializer.Deserialize<WebHooks.Data.WebHookSubscriber>(jsonString);
                if (webHookSubscriber != null)
                {
                    webHookSubscriber = _webHookSubscribers.Add(webHookSubscriber.SubscriberKey, webHookSubscriber.MessageKey, webHookSubscriber.WebHookUrl);
                }
                context.Response.StatusCode = StatusCodes.Status201Created;
                context.Response.ContentType = "application/json";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(webHookSubscriber)));
                return;
            }
            if (context.Request.Method == HttpMethod.Get.ToString())
            {
                string subscriberKey = context.Request.Query["subscriberKey"];
                var webHookSubscription = _webHookSubscribers.GetWebHookBySubscriberKey(subscriberKey);
                if (webHookSubscription == null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(webHookSubscription)));
                return;
            }
            context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Operation not supported"));
        }
    }
}
