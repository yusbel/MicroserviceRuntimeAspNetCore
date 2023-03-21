using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.EmployeeSubdomain.WebHook;
using Microsoft.AspNetCore.Hosting;
using Sample.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Refit;
using Polly;
using Microsoft.Extensions.Logging;

namespace Sample.EmployeeSubdomain.Host
{
    public class HostService
    {
        private readonly string[] _args;
        public HostService(string[] args) 
        {
            _args = args;
        }
        public static HostService Create(string[] args) 
        {
            return new HostService(args);
        }
        public IHostBuilder GetHostBuilder() 
        {
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(_args)
                //called before any other configuration to avoid overriding any services configuration
                
                .ConfigureWebHost(host =>
                {
                    host.UseEnvironment("Development");
                    host.CaptureStartupErrors(true);
                    host.UseStartup<EmployeeAppStartup>();
                    host.UseKestrel(options =>
                    {
                        options.DisableStringReuse = true;
                        options.ListenLocalhost(5500);
                    });
                })
                .ConfigureServices((host, services) =>
                {
                    services.AddEmployeeServiceDependencies(host.Configuration);
                    
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

                    services.AddHttpClient("TestHttpClient", client =>
                    {
                        client.BaseAddress = new Uri("http://www.asp.net");
                    });
                });
            return host;
        }
    }
}
