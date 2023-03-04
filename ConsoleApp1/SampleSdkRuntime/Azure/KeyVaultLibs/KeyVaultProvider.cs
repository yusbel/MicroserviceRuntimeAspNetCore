using Azure;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Sample.Sdk.Core.Exceptions;
using SampleSdkRuntime.Azure.KeyVaultLibs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.KeyVaultLibs
{
    public class KeyVaultProvider : IKeyVaultProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeyVaultProvider> _logger;
        private readonly SecretClient _secretClient;
        private readonly IKeyVaultPolicyProvider _keyVaultPolicyProvider;

        public KeyVaultProvider(IConfiguration configuration,
            ILogger<KeyVaultProvider> logger,
            SecretClient secretClient,
            IKeyVaultPolicyProvider keyVaultPolicyProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _secretClient = secretClient;
            _keyVaultPolicyProvider = keyVaultPolicyProvider;
        }

        public async Task<(bool wasCreated, KeyVaultAccessPolicyParameters? keyVaultAccessPolicyParameters)> 
            CreatePolicy(string tenantId, 
            string resourceId, 
            Application application, 
            ServicePrincipal servicePrincipal, 
            CancellationToken cancellationToken)
        {
            return await _keyVaultPolicyProvider.CreatePolicy(tenantId, resourceId, application, servicePrincipal, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> DeleteAccessPolicy(string tenantId, Guid appId, Guid servicePrincipalId, string resourceIdentifier, CancellationToken cancellationToken)
        {
            return await _keyVaultPolicyProvider.DeleteAccessPolicy(tenantId, appId, servicePrincipalId, resourceIdentifier, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> SaveSecretInKeyVault(string secretKey,
            string secretText,
            int counter,
            CancellationToken cancellationToken)
        {
            KeyVaultSecret? keyVaultSecret;
            try
            {
                keyVaultSecret = _secretClient.SetSecret(new KeyVaultSecret(secretKey, secretText), cancellationToken);
                return true;
            }
            catch (RequestFailedException failedException) when (failedException.ErrorCode == "ObjectIsDeletedButRecoverable")
            {
                await _secretClient.PurgeDeletedSecretAsync(secretKey).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "Setting the secret fail");
                return false;
            }
            if (counter == 3) { return false; }
            counter++;
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            return await SaveSecretInKeyVault(secretKey,
                secretText,
                counter,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
