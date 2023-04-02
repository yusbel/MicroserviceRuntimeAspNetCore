using Sample.Sdk.Data.Constants;
using Sample.Sdk.Interface.Azure.BlobLibs;
using Sample.Sdk.Interface.Registration;
using SampleSdkRuntime.HostedServices.Interfaces;
using SampleSdkRuntime.Providers;
using SampleSdkRuntime.Providers.Data;

namespace SampleSdkRuntime.HostedServices
{
    public class RuntimeSetupHostedAppService : RuntimeHostedServiceBase, IHostedService
    {
        private readonly IConfiguration? _configuration;
        private readonly IRuntimeVerificationService _runtimeVerificationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBlobProvider _blobProvider;
        private CancellationTokenSource? tokenSource;

        public RuntimeSetupHostedAppService(IConfiguration configuration,
            IRuntimeVerificationService runtimeVerificationService,
            IServiceProvider serviceProvider,
            IBlobProvider blobProvider) : base(configuration)
        {
            _configuration = configuration;
            _runtimeVerificationService = runtimeVerificationService;
            _serviceProvider = serviceProvider;
            _blobProvider = blobProvider;
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
            Environment.SetEnvironmentVariable(ConfigVarConst.RUNTIME_SETUP_INFO, string.Empty);
            var ServiceInstanceIdentifier = _configuration.GetValue<string>(ConfigVarConst.SERVICE_INSTANCE_NAME_ID);
            var serviceReg = await ServiceRegistrationProvider.Create(_serviceProvider, ServiceInstanceIdentifier)
                                .ConfigureServiceCredential(token)
                                .ConfigureServiceCryptoSecret(token)
                                .Build(token)
                                .ConfigureAwait(false);
            
            CreateSetupInfo(serviceReg);
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
