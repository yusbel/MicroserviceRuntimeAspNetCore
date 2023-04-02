using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;

namespace SampleSdkRuntime.Providers.RuntimeObservers
{
    public class AppRegVerifictionObserver : IAsyncObserver<RuntimeVerificationEvent, VerificationResult>
    {
        private readonly ILogger<AppRegVerifictionObserver> _logger;

        public AppRegVerifictionObserver(ILogger<AppRegVerifictionObserver> logger)
        {
            _logger = logger;
        }

        public Task<VerificationResult> OnNext(RuntimeVerificationEvent value)
        {
            if (value.VerificationType != VerificationType.ApplicationRegistration && value.VerificationType != VerificationType.NONE)
                return Task.FromResult(new VerificationResult());

            _logger.LogInformation("A runtime event has being raised");
            return Task.FromResult(new VerificationResult());
        }
    }
}
