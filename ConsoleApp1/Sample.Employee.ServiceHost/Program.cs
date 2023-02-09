// See https://aka.ms/new-console-template for more information
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Sdk;
using Sample.Sdk.Msg;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Persistance.Context;
using Microsoft.Extensions.Configuration;
using Sample.EmployeeSubdomain.Service;
using Sample.EmployeeSubdomain;

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
                    services.AddEmployeeServiceDependencies(host.Configuration);
                    services.AddSampleSdk(host.Configuration);
                    services.AddAzureClients(azureClientFactoryBuilder => 
                    {
                        var serviceBusConnStr = host.Configuration.GetValue<string>("EmployeeService:MessageBusInfo:ServiceBusConnStr");
                        azureClientFactoryBuilder.AddServiceBusClient(serviceBusConnStr).ConfigureOptions((options, host) => 
                        {
                            options.Identifier = "EmployeeAddedSenderFromEmployeeService";
                            var configuration = host.GetRequiredService<IConfiguration>();
                            options.RetryOptions = new ServiceBusRetryOptions()
                            {
                                Delay = TimeSpan.FromSeconds(configuration.GetValue<int>("EmployeeService:MessageBusInfo:EmployeeAddedSender:DelayInSeconds")),
                                MaxDelay = TimeSpan.FromSeconds(configuration.GetValue<int>("EmployeeService:MessageBusInfo:EmployeeAddedSender:MaxDelayInSeconds")),
                                MaxRetries = configuration.GetValue<int>("EmployeeService:MessageBusInfo:EmployeeAddedSender:MaxRetries"),
                                Mode = configuration.GetValue<string>("EmployeeService:MessageBusInfo:EmployeeAddedSender:Mode") == "Fixed" ? ServiceBusRetryMode.Fixed : ServiceBusRetryMode.Exponential
                            };
                        });
                    });
                    services.Configure<ServiceBusInfoOptions>(host.Configuration.GetSection("EmployeeService:Employee:MessageBusInfo"));
                })
            .Build();

await Task.Delay(1000);
await RegisterNotifier.WebHook();
Console.WriteLine("========================Employee Service=================================");
await employeeHost.RunAsync();
