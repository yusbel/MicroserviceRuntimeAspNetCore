using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using SampleSdkRuntime.Azure.ActiveDirectoryLibs.AppRegistration;
using SampleSdkRuntime.Data;
using SampleSdkRuntime.Extensions;
using SampleSdkRuntime.HostedServices.Interfaces;
using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;
using SampleSdkRuntime.Providers.Registration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SampleSdkRuntime.HostedServices
{
    public class RuntimeSetupHostedAppService : RuntimeHostedServiceBase, IHostedService
    {
        private readonly IConfiguration? _configuration;
        private readonly IRuntimeVerificationService _runtimeVerificationService;
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource? tokenSource;

        public RuntimeSetupHostedAppService(IConfiguration configuration,
            IRuntimeVerificationService runtimeVerificationService,
            IServiceProvider serviceProvider) : base(configuration)
        {
            _configuration = configuration;
            _runtimeVerificationService = runtimeVerificationService;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return Task.Run(async () => 
            {
                await CreateSetup(tokenSource.Token).ConfigureAwait(false);
                await VerifySetup(tokenSource.Token).ConfigureAwait(false);
            }, tokenSource.Token);
        }

        private async Task CreateSetup(CancellationToken token)
        {
            Environment.SetEnvironmentVariable(ServiceRuntime.RUNTIME_SETUP_INFO, string.Empty);
            var ServiceInstanceIdentifier = _configuration.GetValue<string>(ServiceRuntime.SERVICE_INSTANCE_ID);
            
            //Create application service, service principel, add service pricipal password to key vault and add policy to query for secret
            var serviceRegProvider = _serviceProvider.GetRequiredService<IServiceRegistrationProvider>();
            var serviceReg = await serviceRegProvider.GetServiceRegistration(ServiceInstanceIdentifier, token)
                                                        .ConfigureAwait(false);
            if (serviceReg == null || !serviceReg.WasSuccessful)
            {
                serviceReg = ServiceRegistrationProvider.Create(_serviceProvider)
                                    .ConfigureServiceCredential(ServiceInstanceIdentifier, token)
                                    .ConfigureServiceCryptoSecret(token)
                                    .Build();
            }
            CreateSetupInfo(serviceReg!);
            return;
        }

        private async Task VerifySetup(CancellationToken cancellationToken) 
        {
            while (!cancellationToken.IsCancellationRequested) 
            {
                var results = await _runtimeVerificationService.Verify(new RuntimeVerificationEvent() 
                                        {
                                             VerificationType = VerificationType.NONE
                                        });
                var repairResults = new List<VerificationRepairResult>();
                foreach(var result in results.Where(r=>!r.Success)) 
                {
                    repairResults.AddRange(await _runtimeVerificationService.Repair(result).ConfigureAwait(false));
                }
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if(tokenSource != null) 
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }
            base.Dispose();
            return Task.CompletedTask;
        }
    }
}
