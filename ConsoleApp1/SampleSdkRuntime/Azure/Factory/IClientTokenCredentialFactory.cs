using Azure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.Factory
{
    public interface IClientTokenCredentialFactory
    {
        public bool TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out ClientSecretCredential clientSecretCredential);
        (string tenantId, string clientId, string clientSecret)
            GetDefaultCredential();

        string GetDefaultTenantId();
    }
}
