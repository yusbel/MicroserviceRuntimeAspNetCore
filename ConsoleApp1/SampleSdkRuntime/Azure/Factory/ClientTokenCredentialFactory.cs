using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.Factory
{
    /// <summary>
    /// Factory class for oauth flow implementations
    /// </summary>
    public class ClientTokenCredentialFactory : IClientTokenCredentialFactory
    {
        private readonly IConfiguration _configuration;

        public ClientTokenCredentialFactory(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public bool TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out ClientSecretCredential secretCredential)
        {
            var credentialOption = new TokenCredentialOptions()
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            (string tenantId, string clientId, string clientSecret) = GetDefaultCredential();
            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                secretCredential = default;
                return false;
            }
            secretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, credentialOption);
            return true;
        }

        public string GetDefaultTenantId() 
        {
            return _configuration.GetValue<string>("AZURE_TENANT_ID");
        }

        public (string tenantId, string clientId, string clientSecret)
            GetDefaultCredential()
        {
            return (_configuration.GetValue<string>("AZURE_TENANT_ID"),
                _configuration.GetValue<string>("AZURE_CLIENT_ID"),
                _configuration.GetValue<string>("AZURE_CLIENT_SECRET"));
        }
    }
}
