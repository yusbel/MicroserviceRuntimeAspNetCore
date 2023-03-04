using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Providers
{
    public class RuntimeRepairProvider : IRuntimeRepairProvider
    {
        private readonly List<IAsyncObserver<VerificationResult, VerificationRepairResult>> _observers;
        public RuntimeRepairProvider() 
        {
            _observers = new List<IAsyncObserver<VerificationResult, VerificationRepairResult>>();
        }
        public async Task<IEnumerable<VerificationRepairResult>> Repair(VerificationResult verificationResult)
        {
            var results = new List<VerificationRepairResult>();
            try
            {
                foreach(var observer in _observers) 
                {
                    results.Add(await observer.OnNext(verificationResult).ConfigureAwait(false));
                }
                return results;
            }
            catch (Exception e) { throw; }
        }

        public IDisposable Subscribe(IAsyncObserver<VerificationResult, VerificationRepairResult> observer)
        {
            if(!_observers.Contains(observer)) 
            {
                _observers.Add(observer);
            }
            return new UnSubscribe(_observers, observer);
        }

        public class UnSubscribe : IDisposable
        {
            private readonly List<IAsyncObserver<VerificationResult, VerificationRepairResult>> _observers;
            private readonly IAsyncObserver<VerificationResult, VerificationRepairResult> _observer;

            public UnSubscribe(List<IAsyncObserver<VerificationResult, VerificationRepairResult>> observers, IAsyncObserver<VerificationResult, VerificationRepairResult> observer)
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
