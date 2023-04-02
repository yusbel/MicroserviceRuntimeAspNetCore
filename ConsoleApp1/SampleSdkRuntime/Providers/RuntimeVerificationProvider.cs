using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;

namespace SampleSdkRuntime.Providers
{
    public class RuntimeVerificationProvider : IRuntimeVerificationProvider
    {
        private List<IAsyncObserver<RuntimeVerificationEvent, VerificationResult>> _observers;
        private readonly ILogger<RuntimeVerificationProvider> _logger;

        public RuntimeVerificationProvider(ILogger<RuntimeVerificationProvider> logger)
        {
            _observers = new List<IAsyncObserver<RuntimeVerificationEvent, VerificationResult>>();
            _logger = logger;
        }

        public async Task<IEnumerable<VerificationResult>> Test(RuntimeVerificationEvent verificationEvent)
        {
            var results = new List<VerificationResult>();
            foreach (var observer in _observers)
            {
                try
                {
                    results.Add(await observer.OnNext(verificationEvent).ConfigureAwait(false));
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical, "An error ocurred when invoking an observer");
                }
            }
            return results;
        }

        public IDisposable Subscribe(IAsyncObserver<RuntimeVerificationEvent, VerificationResult> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
            return new UnSubscribe(_observers, observer);
        }

        public class UnSubscribe : IDisposable
        {
            private readonly List<IAsyncObserver<RuntimeVerificationEvent, VerificationResult>> _observers;
            private readonly IAsyncObserver<RuntimeVerificationEvent, VerificationResult> _observer;

            public UnSubscribe(List<IAsyncObserver<RuntimeVerificationEvent, VerificationResult>> observers,
                                IAsyncObserver<RuntimeVerificationEvent, VerificationResult> observer)
            {
                _observers = observers;
                _observer = observer;
            }
            public void Dispose()
            {
                if (_observers != null && _observer != null)
                {
                    _observers.Remove(_observer);
                }
            }
        }
    }
}
