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
