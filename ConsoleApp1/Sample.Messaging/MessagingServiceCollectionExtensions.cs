using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Polly.Wrap;
using Sample.Messaging.Publishers;
using Sample.Messaging.WebHooks;
using Sample.Sdk.InMemory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging
{
    public static class MessagingServiceCollectionExtensions
    {
        public static IServiceCollection AddMessagingServices(this IServiceCollection services)
        {
            services.AddSingleton<IWebHookSubscription, WebHookSubscription>();
            services.AddSingleton<IWebHookMessageSubscription, WebHookMessageSubscription>();
            services.AddTransient<IWebHookPublisher, WebHookPublisher>();
            services.AddTransient<IMessagePublisher, MessagePublisher>();

            //WebHookPublisher will use http client to send short lived request concurrently
            var circuitBreakerDefaultPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(5));


            services.AddHttpClient<WebHookPublisher>(client =>
            {

            })
            .AddTypedClient<WebHookPublisher>()
            .AddPolicyHandler(rquest =>
            {
                return HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(5, TimeSpan.FromSeconds(5));
            });
            return services;
        }
    }
}
