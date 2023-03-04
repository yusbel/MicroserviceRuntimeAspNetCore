using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Exceptions;
using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
