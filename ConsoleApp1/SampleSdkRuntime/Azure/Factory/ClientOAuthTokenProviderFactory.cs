using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using SampleSdkRuntime.Azure.Factory.Interfaces;
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
    public class ClientOAuthTokenProviderFactory : IClientOAuthTokenProviderFactory
    {
        private readonly IConfiguration _configuration;

        public ClientOAuthTokenProviderFactory(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public OnBehalfOfCredential GetOnBehalfOfCredential(string accessToken) 
        {
            (string TenantId, string ClientId, string ClientSecret) = GetAzureServiceInstanceCredential();
            var credential = new OnBehalfOfCredential(TenantId, ClientId, ClientSecret, accessToken, new OnBehalfOfCredentialOptions() 
            {
                 AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            });
            return credential;
        }
        public bool TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out ClientSecretCredential secretCredential)
        {
            var credentialOption = new TokenCredentialOptions()
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            (string TenantId, string ClientId, string ClientSecret) = _configuration.GetValue<bool>(ServiceRuntime.IS_RUNTIME) 
                                                                        ? GetAzureRuntimeServiceCredential() 
                                                                        : GetAzureServiceInstanceCredential();
            if (string.IsNullOrEmpty(TenantId) || string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
            {
                secretCredential = default;
                return false;
            }
            var clientSecretCredentialOptions = new ClientSecretCredentialOptions() 
            { 
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            secretCredential = new ClientSecretCredential(TenantId, ClientId, ClientSecret, clientSecretCredentialOptions);
            return true;
        }

        public string GetDefaultTenantId() 
        {
            return _configuration.GetValue<bool>(ServiceRuntime.IS_RUNTIME) ? _configuration.GetValue<string>(ServiceRuntime.RUNTIME_AZURE_TENANT_ID)
                                                                            : _configuration.GetValue<string>(ServiceRuntime.AZURE_TENANT_ID);
        }

        public (string TenantId, string ClientId, string ClientSecret)
            GetAzureTokenCredentials()
        {
            return _configuration.GetValue<bool>(ServiceRuntime.IS_RUNTIME)
                                                                        ? GetAzureRuntimeServiceCredential()
                                                                        : GetAzureServiceInstanceCredential();
        }

        public (string TenantId, string ClientId, string ClientSecret)
        GetAzureServiceInstanceCredential()
        {
            return (_configuration.GetValue<string>(ServiceRuntime.AZURE_TENANT_ID),
                    _configuration.GetValue<string>(ServiceRuntime.AZURE_CLIENT_ID),
                    _configuration.GetValue<string>(ServiceRuntime.AZURE_CLIENT_SECRET));
                    
        }

        public (string TenantId, string ClientId, string ClientSecret)
            GetAzureRuntimeServiceCredential()
        {
            return (_configuration.GetValue<string>(ServiceRuntime.RUNTIME_AZURE_TENANT_ID),
                            _configuration.GetValue<string>(ServiceRuntime.RUNTIME_AZURE_CLIENT_ID),
                            _configuration.GetValue<string>(ServiceRuntime.RUNTIME_AZURE_CLIENT_SECRET));
        }
    }
}
