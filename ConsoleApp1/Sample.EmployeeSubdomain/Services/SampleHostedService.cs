using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Services
{
    public class SampleHostedService : IHostedService
    {
        private readonly ILogger<SampleHostedService> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public SampleHostedService(
            ILogger<SampleHostedService> logger, 
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested) 
            {
                Console.WriteLine("Service is working");
                await Task.Delay(1000);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is being cancel");
            return Task.CompletedTask;
        }
    }
}
