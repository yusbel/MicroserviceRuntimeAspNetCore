// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ConsoleApp1;

Console.WriteLine($"Processor count ${Environment.ProcessorCount}");

Console.WriteLine("Hello World");
//var logger = LoggerFactory.Create((builder) => { builder.AddConsole(); }).CreateLogger("");

//var hosts = new List<Task>();

//IWebHost messagingWebHost = await MessagingStarterHost.ConfigureMessaging(args, logger, hosts);

//await Task.Delay(5000);

//IHost employeeHost = await EmployeeStarterHost.ConfigureEmployeeService(args, hosts);

//await Task.Delay(5000);

//PayRollStarterHost.ConfigurePayRollService(args, hosts);

//await Task.WhenAll(hosts);




