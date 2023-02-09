using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.EmployeeSubdomain;
using Sample.EmployeeSubdomain.Service;
using Sample.EmployeeSubdomain.Service.DatabaseContext;
using Sample.EmployeeSubdomain.Service.Entities;
using Sample.EmployeeSubdomain.Service.Interfaces;
using Sample.EmployeeSubdomain.Service.Services;
using Sample.EmployeeSubdomain.Service.Services.Interfaces;
using Sample.Sdk;
using Sample.Sdk.Msg;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ConsoleApp1
{
    /// <summary>
    /// Move
    /// </summary>
    public class EmployeeStarterHost
    {
        public static async Task<IHost> ConfigureEmployeeService(string[] args, List<Task> hosts)
        {
            ////Employee
            //IHost employeeHost = Host.CreateDefaultBuilder(args)
            //    .ConfigureWebHost(webHost =>
            //    {
            //        webHost.UseEnvironment("Development");
            //        webHost.CaptureStartupErrors(true);
            //        webHost.UseStartup<EmployeeAppStartup>();
            //        webHost.UseKestrel(options =>
            //        {
            //            options.DisableStringReuse = true;
            //            options.ListenLocalhost(5500);
            //        });
            //    })
            //    .ConfigureServices((host, services) =>
            //    {
            //        services.AddTransient<IEmployee, Employee>();
            //        services.AddTransient<IEntityContext<EmployeeContext, EmployeeEntity>, EntityContext<EmployeeContext, EmployeeEntity>>();
            //        services.AddDbContext<EmployeeContext>(options =>
            //        {
            //            options.EnableDetailedErrors(true);
            //        });
            //        services.AddEmployeeServiceDependencies(host.Configuration);
            //        services.AddSampleSdk();
            //        services.AddTransient<IEmployeeAppService, EmployeeAppService>();
            //        services.AddTransient<IMessageBusSender, ServiceBusMessageSender>();
            //        services.AddHostedService<EmployeeHostedService>();
            //        services.Configure<ServiceBusInfoOptions>(host.Configuration.GetSection("EmployeeService:Employee:MsgWithDbTransaction"));
            //    })
            //.Build();

            ////Employee subscribe to webhook notifications
            //await RegisterNotifier.WebHook();
            //hosts.Add(employeeHost.RunAsync());
            //return employeeHost;
            return null;
        }
    }
}
