using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sample.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Host
{
    public static class HostService
    {
        public static IHostBuilder CreateGenericHost(string[] args) 
        {
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                                .ConfigureWebHost(host =>
                                {
                                    host.CaptureStartupErrors(true);
                                    host.UseStartup<PayRollStartUp>();
                                    host.UseKestrel(configOptions => 
                                    {
                                        configOptions.ListenAnyIP(5400);
                                    });
                                }).ConfigureServices((host,services) => 
                                {
                                    services.AddPayRollServiceDependencies(host.Configuration);
                                });
            return host;
        }
    }
}
