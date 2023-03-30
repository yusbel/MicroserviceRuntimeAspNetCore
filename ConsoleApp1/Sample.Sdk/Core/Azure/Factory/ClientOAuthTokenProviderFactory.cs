using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Sample.Sdk.Core.Azure.Factory.Interfaces;
using Sample.Sdk.Core.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Azure.Factory
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

        public ClientSecretCredential GetClientSecretCredential() 
        {
            var clientSecretCredentialOptions = new ClientSecretCredentialOptions() 
            {
                 AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            return new ClientSecretCredential(
                Environment.GetEnvironmentVariable(ConfigurationVariableConstant.AZURE_TENANT_ID),
                Environment.GetEnvironmentVariable(ConfigurationVariableConstant.AZURE_CLIENT_ID),
                Environment.GetEnvironmentVariable(ConfigurationVariableConstant.AZURE_CLIENT_SECRET),
                clientSecretCredentialOptions);
        }
        public bool TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out ClientSecretCredential secretCredential)
        {
            (string TenantId, string ClientId, string ClientSecret) = _configuration.GetValue<bool>(ConfigurationVariableConstant.IS_RUNTIME)
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
            return _configuration.GetValue<bool>(ConfigurationVariableConstant.IS_RUNTIME) 
                                                ? _configuration.GetValue<string>(ConfigurationVariableConstant.RUNTIME_AZURE_TENANT_ID)
                                                : _configuration.GetValue<string>(ConfigurationVariableConstant.AZURE_TENANT_ID);
        }

        public (string TenantId, string ClientId, string ClientSecret)
            GetAzureTokenCredentials()
        {
            return _configuration.GetValue<bool>(ConfigurationVariableConstant.IS_RUNTIME)
                                                                        ? GetAzureRuntimeServiceCredential()
                                                                        : GetAzureServiceInstanceCredential();
        }

        public (string TenantId, string ClientId, string ClientSecret)
        GetAzureServiceInstanceCredential()
        {
            return (_configuration.GetValue<string>(ConfigurationVariableConstant.AZURE_TENANT_ID),
                    _configuration.GetValue<string>(ConfigurationVariableConstant.AZURE_CLIENT_ID),
                    _configuration.GetValue<string>(ConfigurationVariableConstant.AZURE_CLIENT_SECRET));

        }

        public (string TenantId, string ClientId, string ClientSecret)
            GetAzureRuntimeServiceCredential()
        {
            if (!string.IsNullOrEmpty(_configuration.GetValue<string>(ConfigurationVariableConstant.RUNTIME_AZURE_CLIENT_ID))) 
            {
                return (_configuration.GetValue<string>(ConfigurationVariableConstant.RUNTIME_AZURE_TENANT_ID),
                            _configuration.GetValue<string>(ConfigurationVariableConstant.RUNTIME_AZURE_CLIENT_ID),
                            _configuration.GetValue<string>(ConfigurationVariableConstant.RUNTIME_AZURE_CLIENT_SECRET));
            }
            return (Environment.GetEnvironmentVariable(ConfigurationVariableConstant.RUNTIME_AZURE_TENANT_ID)!,
                            Environment.GetEnvironmentVariable(ConfigurationVariableConstant.RUNTIME_AZURE_CLIENT_ID)!,
                            Environment.GetEnvironmentVariable(ConfigurationVariableConstant.RUNTIME_AZURE_CLIENT_SECRET)!);
        }
    }
}
