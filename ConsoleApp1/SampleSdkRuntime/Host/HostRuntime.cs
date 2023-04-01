using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Host
{
    public class HostRuntime
    {
        public IHostBuilder Create(string[] args) 
        {
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                            .ConfigureWebHost(host =>
                                                {
                                                });
            return host;
        }
    }
}
