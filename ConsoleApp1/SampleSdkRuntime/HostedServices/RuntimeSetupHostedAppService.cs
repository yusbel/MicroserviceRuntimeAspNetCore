using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using SampleSdkRuntime.Azure.ActiveDirectoryLibs.AppRegistration;
using SampleSdkRuntime.Data;
using SampleSdkRuntime.HostedServices.Interfaces;
using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;
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
        private readonly IApplicationRegistration? _applicationRegistration;
        private readonly IConfiguration? _configuration;
        private readonly IRuntimeVerificationService _runtimeVerificationService;
        private CancellationTokenSource? tokenSource;

        public RuntimeSetupHostedAppService(IApplicationRegistration applicationRegistration,
            IConfiguration configuration,
            IRuntimeVerificationService runtimeVerificationService) : base(configuration)
        {
            _applicationRegistration = applicationRegistration;
            _configuration = configuration;
            _runtimeVerificationService = runtimeVerificationService;
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
            var setupInfo = new SetupInfo()
            {
                ServiceInstanceIdentifier = _configuration.GetValue<string>(ServiceRuntime.SERVICE_INSTANCE_ID)
            };
            //Create application service, service principel, add service pricipal password to key vault and add policy to query for secret
            (bool wasSuccess, Application? app, ServicePrincipal? principal, string? clientSecret) appSetupInfo =
                    await _applicationRegistration!.GetApplicationDetails(setupInfo.ServiceInstanceIdentifier, token)
                                                    .ConfigureAwait(false);
            if (!appSetupInfo.wasSuccess) 
            {
                appSetupInfo = await _applicationRegistration.DeleteAndCreate(setupInfo.ServiceInstanceIdentifier, token)
                                                    .ConfigureAwait(false);
            }
            if(appSetupInfo.wasSuccess) 
            {
                CreateSetupInfo(setupInfo, appSetupInfo.wasSuccess, appSetupInfo.app, appSetupInfo.clientSecret);
            }
            throw new InvalidOperationException("Runtime was unable to setup the service instance");
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
