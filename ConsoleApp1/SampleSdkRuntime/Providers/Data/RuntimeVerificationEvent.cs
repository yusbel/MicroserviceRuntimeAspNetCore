using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Providers.Data
{
    public enum VerificationType 
    {
        NONE, 
        ApplicationRegistration, 
        ServicePrincipal, 
        KeyVaultSecret, 
        KeyVaultPolicy
    }
    public class RuntimeVerificationEvent
    {
        public VerificationType VerificationType { get; init; }
    }
}
