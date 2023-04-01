
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Text;

namespace Sample.EmployeeSubdomain
{
    public class EmployeeAppStartup
    {
        private readonly IConfiguration _configuration;

        public EmployeeAppStartup(IConfiguration configuration) => _configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            //RegisterNotifier.Register();
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {   
            //app.UseMiddleware<CryptoMiddleware>();  
            //app.UseMiddleware<CustomSecureTransparentEncryptionMiddleware>();
            //app.UseMiddleware<CustomProtocolAcknowledgementMiddleware>();

            //app.UseMiddleware<WebHookMiddleware>();//would be replaced with EventGrid

            app.Run(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Employee app http for webhook calls invoked"));
            });
        }
    }
}
