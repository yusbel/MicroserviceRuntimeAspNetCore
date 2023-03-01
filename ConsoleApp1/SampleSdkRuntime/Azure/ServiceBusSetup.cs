using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure
{
    public class ServiceBusSetup
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public ServiceBusSetup(
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        public async Task RunAsync(string serviceInstanceId) 
        {
            await SetupQueues();
        }

        private Task SetupQueues()
        {
            throw new NotImplementedException();
        }
    }
}
