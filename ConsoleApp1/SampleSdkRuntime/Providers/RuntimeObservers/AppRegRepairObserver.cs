using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Providers.RuntimeObservers
{
    public class AppRegRepairObserver : IAsyncObserver<VerificationResult, VerificationRepairResult>
    {
        public Task<VerificationRepairResult> OnNext(VerificationResult value)
        {
            return Task.FromResult(new VerificationRepairResult() { Success = true });
        }
    }
}
