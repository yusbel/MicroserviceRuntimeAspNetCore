using SampleSdkRuntime.Providers.Data;

namespace SampleSdkRuntime.Providers.Interfaces
{
    public interface IRuntimeRepairProvider : IAsyncObservable<VerificationResult, VerificationRepairResult>
    {
        Task<IEnumerable<VerificationRepairResult>> Repair(VerificationResult verificationResult);
    }
}