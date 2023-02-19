using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Messaging.Middleware;
using Sample.Messaging.Publishers;
using Sample.Messaging.WebHooks;
using Sample.Sdk.InMemory;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging
{
    public class WebAppStartUp
    {
        private IConfiguration _configuration;

        public WebAppStartUp(IConfiguration configuration) => _configuration = configuration;
        public void ConfigureServices(IServiceCollection services) 
        {
            

            
        }

        public void Configure(IApplicationBuilder appBuilder, IWebHostEnvironment env) 
        {
            appBuilder.UseMiddleware<WebHookMiddleware>();

            appBuilder.UseMiddleware<MessageMiddleware>();

            appBuilder.Run(async (context) =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));
            });
        }

    }
}
