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
                    services.AddSampleSdk(host.Configuration);
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

await Task.Delay(1000);
await RegisterNotifier.WebHook();

Console.WriteLine("========================Employee Service=================================");
await employeeHost.RunAsync();
