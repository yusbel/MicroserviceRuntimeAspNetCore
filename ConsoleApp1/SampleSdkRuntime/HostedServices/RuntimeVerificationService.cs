using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;
using SampleSdkRuntime.HostedServices.Interfaces;

namespace SampleSdkRuntime.HostedServices
{
    public class RuntimeVerificationService : IRuntimeVerificationService
    {
        private readonly IRuntimeVerificationProvider _runtimeVerification;
        private readonly IRuntimeRepairProvider _runtimeRepairProvider;

        public RuntimeVerificationService(IEnumerable<IAsyncObserver<RuntimeVerificationEvent, VerificationResult>> observers,
            IRuntimeVerificationProvider runtimeVerification,
            IEnumerable<IAsyncObserver<VerificationResult, VerificationRepairResult>> repairObservers,
            IRuntimeRepairProvider runtimeRepairProvider)
        {
            _runtimeVerification = runtimeVerification;
            _runtimeRepairProvider = runtimeRepairProvider;
            foreach (var observer in observers)
            {
                _runtimeVerification.Subscribe(observer);
            }
            foreach (var repairObserver in repairObservers)
            {
                _runtimeRepairProvider.Subscribe(repairObserver);
            }
        }

        public async Task<IEnumerable<VerificationResult>> Verify(RuntimeVerificationEvent verificationEvent)
        {
            try
            {
                return await _runtimeVerification.Test(verificationEvent).ConfigureAwait(false);
            }
            catch (Exception e) { throw; }
        }

        public async Task<IEnumerable<VerificationRepairResult>> Repair(VerificationResult verificationResult)
        {
            try
            {
                return await _runtimeRepairProvider.Repair(verificationResult).ConfigureAwait(false);
            }
            catch (Exception e)
            { throw; }
        }
    }
}
