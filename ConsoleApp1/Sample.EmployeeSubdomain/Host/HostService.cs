using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Refit;

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
