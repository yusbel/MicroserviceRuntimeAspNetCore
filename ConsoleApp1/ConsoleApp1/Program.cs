// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ConsoleApp1;
using SampleSdkRuntime;
using Sample.EmployeeSubdomain.Host;
using Microsoft.Extensions.DependencyInjection.Extensions;

var services = new ServiceCollection();
services.AddTransient<ICustomLogger, CustomLogger>();
services.AddTransient<ICustomLogger, CustomDateTimeLogger>();


//Console.WriteLine($"Processor count ${Environment.ProcessorCount}");

//string[] serviceArgs = new string[] { "EmployeeService-0123456789" };

//var employeeHost = HostService.Create(serviceArgs);
//await ServiceRuntime.RunAsync(serviceArgs, employeeHost.GetHostBuilder()).ConfigureAwait(false);

//Console.ReadKey();
//var logger = LoggerFactory.Create((builder) => { builder.AddConsole(); }).CreateLogger("");

//var hosts = new List<Task>();

//IWebHost messagingWebHost = await MessagingStarterHost.ConfigureMessaging(args, logger, hosts);

//await Task.Delay(5000);

//IHost employeeHost = await EmployeeStarterHost.ConfigureEmployeeService(args, hosts);

//await Task.Delay(5000);

//PayRollStarterHost.ConfigurePayRollService(args, hosts);

//await Task.WhenAll(hosts);




