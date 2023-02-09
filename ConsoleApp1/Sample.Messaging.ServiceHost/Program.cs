using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Sample.Messaging;

IWebHost messagingWebHost = WebHost.CreateDefaultBuilder(args)
                                                .ConfigureServices(services => { })
                                                .CaptureStartupErrors(true)
                                                .UseStartup<WebAppStartUp>()
                                                .UseKestrel(options =>
                                                {
                                                    options.ListenLocalhost(5400, listenOptions =>
                                                    {
                                                        listenOptions.UseConnectionLogging();
                                                    });
                                                })
                                                .Build();
Console.WriteLine("==================Messaging Service=======================");
await messagingWebHost.RunAsync();
