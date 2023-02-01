using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.EmployeeSubdomain.Employee;
using Sample.EmployeeSubdomain.Employee.DatabaseContext;
using Sample.EmployeeSubdomain.Employee.Entities;
using Sample.EmployeeSubdomain.Employee.Interfaces;
using Sample.EmployeeSubdomain.Employee.Services;
using Sample.Sdk;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class EmployeeStarterHost
    {
        public static async Task<IHost> ConfigureEmployeeService(string[] args, List<Task> hosts)
        {
            //Employee
            IHost employeeHost = Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseEnvironment("Development");
                    webHost.CaptureStartupErrors(true);
                    webHost.UseStartup<EmployeeAppStartup>();
                    webHost.UseKestrel(options =>
                    {
                        options.DisableStringReuse = true;
                        options.ListenLocalhost(5500);
                    });
                })
                .ConfigureServices((host, services) =>
                {
                    services.AddTransient<IEmployee, Employee>();
                    services.AddTransient<IEntityContext<EmployeeContext, EmployeeEntity>, EntityContext<EmployeeContext, EmployeeEntity>>();
                    services.AddDbContext<EmployeeContext>(options =>
                    {
                        options.EnableDetailedErrors(true);
                    });
                    services.AddEmployeeServiceDependency(host.Configuration);
                    services.AddSampleSdk();
                    services.AddScoped<IEmployeeAppService, EmployeeAppService>();
                    services.AddHostedService<EmployeeHostApp>();
                })
            .Build();

            //Employee subscribe to webhook notifications
            await Sample.EmployeeSubdomain.Employee.RegisterNotifier.WebHook();
            hosts.Add(employeeHost.RunAsync());
            return employeeHost;
        }
    }
}
