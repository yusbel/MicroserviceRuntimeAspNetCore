using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;

namespace SampleSdkRuntime.Providers.RuntimeObservers
{
    public class ServicePrincipalVerificationObserver : IAsyncObserver<RuntimeVerificationEvent, VerificationResult>
    {
        private readonly ILogger<ServicePrincipalVerificationObserver> _logger;

        public ServicePrincipalVerificationObserver(ILogger<ServicePrincipalVerificationObserver> logger)
        {
            _logger = logger;
        }

        public Task<VerificationResult> OnNext(RuntimeVerificationEvent value)
        {
            if (value.VerificationType != VerificationType.ServicePrincipal && value.VerificationType != VerificationType.NONE)
                return Task.FromResult(new VerificationResult());

            _logger.LogInformation("A new verification event has been raised");
            return Task.FromResult(new VerificationResult());
        }
    }
}
