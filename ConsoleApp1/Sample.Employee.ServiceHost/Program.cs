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
using Sample.EmployeeSubdomain;
using Refit;
using Sample.EmployeeSubdomain.WebHook;
using Grpc.Core;
using Polly;
using System.Reflection.PortableExecutable;

///$Env: AZURE_CLIENT_ID = "51df4bce-6532-4345-9be7-5be7af315003"
/// $Env:AZURE_CLIENT_SECRET="tdm8Q~Cw_e7cLFadttN7Zebacx_kC5Y-0xaWZdv2"
/// $Env:AZURE_TENANT_ID="c8656f45-daf5-42c1-9b29-ac27d3e63bf3"

Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "51df4bce-6532-4345-9be7-5be7af315003");
Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "tdm8Q~Cw_e7cLFadttN7Zebacx_kC5Y-0xaWZdv2");
Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "c8656f45-daf5-42c1-9b29-ac27d3e63bf3");


IHost employeeHost = Host.CreateDefaultBuilder(args)
                //called before any other configuration to avoid overriding any services configuration
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
                    services.AddSampleSdk(host.Configuration,"Employee:AzureServiceBusInfo:Configuration");
                    services.AddRefitClient<WebHookConfiguration>()
                                    .ConfigureHttpClient(client =>
                                    {

                                    })
                                    .AddTransientHttpErrorPolicy((builder) =>
                                        {
                                                builder.OrResult(resp => resp.StatusCode == System.Net.HttpStatusCode.GatewayTimeout);
                                                return builder.WaitAndRetryAsync(new[]
                                                                    {
                                                                        TimeSpan.FromSeconds(1),
                                                                        TimeSpan.FromSeconds(5),
                                                                        TimeSpan.FromSeconds(10)
                                                                    });
                                        });
                    services.AddHttpClient("", client => { });
                })
            .Build();

//await Task.Delay(1000);
//await RegisterNotifier.WebHook();

Console.WriteLine("========================Employee Service=================================");
await employeeHost.RunAsync();
