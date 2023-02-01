// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Sample.Sdk;
using Sample.EmployeeSubdomain.Employee;
using Sample.EmployeeSubdomain.Employee.Interfaces;
using Sample.EmployeeSubdomain.Employee.Messages;
using Sample.Sdk.Persistance.Context;
using Sample.EmployeeSubdomain.Employee.Entities;
using Sample.EmployeeSubdomain.Employee.DatabaseContext;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Sample.Messaging;
using Sample.EmployeeSubdomain.Employee.Services;
using Microsoft.AspNetCore.Builder;
using Sample.EmployeeSubdomain.Employee.Middleware;
using ConsoleApp1;

var logger = LoggerFactory.Create((builder) => { builder.AddConsole(); }).CreateLogger("");

var hosts = new List<Task>();

IWebHost messagingWebHost = await MessagingStarterHost.ConfigureMessaging(args, logger, hosts);

await Task.Delay(5000);

IHost employeeHost = await EmployeeStarterHost.ConfigureEmployeeService(args, hosts);

await Task.Delay(5000);

PayRollStarterHost.ConfigurePayRollService(args, hosts);

await Task.WhenAll(hosts);

