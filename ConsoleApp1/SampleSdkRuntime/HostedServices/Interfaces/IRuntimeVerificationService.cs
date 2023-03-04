using SampleSdkRuntime.Providers.Data;

namespace SampleSdkRuntime.HostedServices.Interfaces
{
    public interface IRuntimeVerificationService
    {
        Task<IEnumerable<VerificationRepairResult>> Repair(VerificationResult verificationResult);
        Task<IEnumerable<VerificationResult>> Verify(RuntimeVerificationEvent verificationEvent);
    }
}