using Microsoft.Extensions.Hosting;
using SampleSdkRuntime.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SampleSdkRuntime
{
    public class RuntimeSetupHostedAppService : IHostedService
    {
        private CancellationTokenSource tokenSource;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Environment.SetEnvironmentVariable("SetupInfo", Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new SetupInfo()))));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
