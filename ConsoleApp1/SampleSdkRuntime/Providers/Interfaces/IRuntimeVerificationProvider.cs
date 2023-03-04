using SampleSdkRuntime.Providers.Data;

namespace SampleSdkRuntime.Providers.Interfaces
{
    public interface IRuntimeVerificationProvider : IAsyncObservable<RuntimeVerificationEvent, VerificationResult>
    {
        Task<IEnumerable<VerificationResult>> Test(RuntimeVerificationEvent verificationEvent);
    }
}