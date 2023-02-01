
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Employee.DatabaseContext;
using Sample.EmployeeSubdomain.Employee.Entities;
using Sample.EmployeeSubdomain.Employee.Interfaces;
using Sample.EmployeeSubdomain.Employee.Middleware;
using Sample.Sdk;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Employee
{
    public class EmployeeAppStartup
    {
        private readonly IConfiguration _configuration;

        public EmployeeAppStartup(IConfiguration configuration) => (_configuration) = (configuration);

        public void ConfigureServices(IServiceCollection services)
        {
            RegisterNotifier.Register();
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) 
        {
            app.UseMiddleware<WebHookMiddleware>();


            app.Run(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Employee app http for webhook calls invoked"));
            });
        }
    }
}
