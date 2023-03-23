using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk.Core.Http.Middleware;
using System.Net;

namespace Sample.PayRoll.Host
{
    public class PayRollStartUp
    {
        public void ConfigureServices(IServiceCollection services) 
        {
            //Services are registered on the configuration of the host builder
        }

        public void Configure(IApplicationBuilder appBuilder, IWebHostEnvironment environment) 
        {
            appBuilder.UseMiddleware<CryptoMiddleware>();

            appBuilder.Run(async (ctx) => 
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("Payroll services response");
            });
        }
    }
}