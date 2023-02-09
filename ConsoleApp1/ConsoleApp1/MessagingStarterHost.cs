using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class MessagingStarterHost
    {
        public static Task<IWebHost> ConfigureMessaging(string[] args, ILogger logger, List<Task> hosts)
        {
            //logger.LogInformation("Configuring employee service");
            //IWebHost messagingWebHost = WebHost.CreateDefaultBuilder(args)
            //                                    .ConfigureServices(services => { })
            //                                    .CaptureStartupErrors(true)
            //                                    .UseStartup<WebAppStartUp>()
            //                                    .UseKestrel(options =>
            //                                    {
            //                                        options.ListenLocalhost(5400, listenOptions =>
            //                                        {
            //                                            listenOptions.UseConnectionLogging();
            //                                        });
            //                                    })
            //                                    .Build();

            //hosts.Add(messagingWebHost.RunAsync());
            //return Task.FromResult(messagingWebHost);
            return null;
        }
    }
}
