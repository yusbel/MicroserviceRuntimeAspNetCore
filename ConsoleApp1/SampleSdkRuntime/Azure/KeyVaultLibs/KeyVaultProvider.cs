using Azure;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
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
        private readonly KeyClient _keyClient;

        public KeyVaultProvider(IConfiguration configuration,
            ILogger<KeyVaultProvider> logger,
            SecretClient secretClient,
            IKeyVaultPolicyProvider keyVaultPolicyProvider,
            KeyClient keyClient)
        {
            _configuration = configuration;
            _logger = logger;
            _secretClient = secretClient;
            _keyVaultPolicyProvider = keyVaultPolicyProvider;
            _keyClient = keyClient;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="resourceId"></param>
        /// <param name="application"></param>
        /// <param name="servicePrincipal"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        public async Task<(bool wasCreated, KeyVaultAccessPolicyParameters? keyVaultAccessPolicyParameters)> 
            CreatePolicy(string tenantId, 
            string resourceId, 
            Application application, 
            ServicePrincipal servicePrincipal, 
            CancellationToken cancellationToken)
        {
            return await _keyVaultPolicyProvider.CreatePolicy(tenantId, resourceId, application, servicePrincipal, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete access policy
        /// </summary>
        /// <param name="tenantId">tenant id</param>
        /// <param name="appId">application id, use appId property</param>
        /// <param name="servicePrincipalId">use the principal property Id</param>
        /// <param name="resourceIdentifier">use the resource identifier that include the subscrition id and the resource group</param>
        /// <param name="cancellationToken">to can cel operation</param>
        /// <returns></returns>
        public async Task<bool> DeleteAccessPolicy(string tenantId, 
            Guid appId, 
            Guid servicePrincipalId, 
            string resourceIdentifier,
            CancellationToken cancellationToken)
        {
            return await _keyVaultPolicyProvider.DeleteAccessPolicy(tenantId, appId, servicePrincipalId, resourceIdentifier, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create oct key for Aes algorithm
        /// </summary>
        /// <param name="keyOptions"></param>
        /// <param name="token"></param>
        /// <param name="createOrDeleteKey"></param>
        /// <param name="counter"></param>
        /// <param name="maxRetry"></param>
        /// <returns></returns>
        public async Task<(bool wasSaved, KeyVaultKey keyVaultKey)>
           CreateOrDeleteOctKeyWithRetry(CreateOctKeyOptions keyOptions,
                                   CancellationToken token,
                                   Func<CreateOctKeyOptions, CancellationToken, Task<KeyVaultKey>> createOrDeleteKey,
                                   int counter = 0,
                                   int maxRetry = 3)
        {
            KeyVaultKey? key = null;
            try
            {
                key = await createOrDeleteKey.Invoke(keyOptions, token).ConfigureAwait(false);
                return (true, key);
            }
            catch (RequestFailedException e) when (e.ErrorCode == "Conflict")
            {
                try
                {
                    await _keyClient.PurgeDeletedKeyAsync(keyOptions.Name, token).ConfigureAwait(false);
                }
                catch (Exception) { }
            }
            catch (Exception e)
            {
                if (counter == maxRetry)
                {
                    e.LogException(_logger.LogCritical);
                    throw;
                }
            }
            if (counter == maxRetry) 
            {
                return (false, default);
            }
            counter++;
            await Task.Delay(1000, token).ConfigureAwait(false);
            return await CreateOrDeleteOctKeyWithRetry(
                                keyOptions,
                                token,
                                createOrDeleteKey,
                                counter,
                                maxRetry)
                                .ConfigureAwait(false);
        }


        /// <summary>
        /// Create key in keyvault with retry using maxretry on an interval
        /// </summary>
        /// <param name="keyName">key name</param>
        /// <param name="keyType">key type</param>
        /// <param name="keyOptions">creation key options</param>
        /// <param name="token">operantion token to cancel</param>
        /// <param name="createOrDeleteKey">operation to invoke with keyname key type and creation key options</param>
        /// <param name="counter"></param>
        /// <returns></returns>
        /// <exception cref="RequestFailedException">raised when maxretry is reached</exception>
        public async Task<(bool wasSaved, KeyVaultKey keyVaultKey)> 
            CreateOrDeleteKeyInKeyVaultWithRetry(string keyName,
                                    KeyType keyType,
                                    CreateKeyOptions keyOptions,
                                    CancellationToken token,
                                    Func<string, KeyType, CreateKeyOptions, Task<KeyVaultKey>> createOrDeleteKey,
                                    int counter = 0,
                                    int maxRetry = 3)
        {
            KeyVaultKey? key = null;
            try
            {
                key = await createOrDeleteKey.Invoke(keyName, keyType, keyOptions).ConfigureAwait(false);
                return (true, key);
            }
            catch (RequestFailedException e) when (e.ErrorCode == "Conflict")
            {
                try
                {
                    await _keyClient.PurgeDeletedKeyAsync(keyName, token).ConfigureAwait(false);
                }
                catch (Exception) 
                {
                    throw;
                }
            }
            catch (Exception e)
            {
                if (counter == maxRetry)
                {
                    e.LogException(_logger.LogCritical);
                    throw;
                }
            }
            counter++;
            await Task.Delay(1000, token).ConfigureAwait(false);
            return await CreateOrDeleteKeyInKeyVaultWithRetry(keyName,
                                keyType,
                                keyOptions,
                                token,
                                createOrDeleteKey,
                                counter, 
                                maxRetry)
                                .ConfigureAwait(false);
        }

        /// <summary>
        /// Use this method for saving and deleting key, it will retry on an interval for the mount of maxretry
        /// </summary>
        /// <param name="secretKey">secret key to identify the secret</param>
        /// <param name="secretText">secret to be saved in keyvault</param>
        /// <param name="counter">invoke the operation with value of zero</param>
        /// <param name="maxRetry">amount of retry before failing</param>
        /// <param name="saveOrDeleteSecret">first string contain the secretKey, secons string contain the secret value</param>
        /// <param name="cancellationToken">cancel the operation</param>
        /// <exception cref="RequestFailedException">throw when max of retry is reached without success</exception> 
        /// <returns></returns>
        public async Task<(bool WasSaved, KeyVaultSecret? Secret)> 
            SaveOrDeleteSecretInKeyVaultWithRetry(string secretKey,
                            string secretText,
                            Func<string, string, Task<KeyVaultSecret>> saveOrDeleteSecret,
                            CancellationToken cancellationToken,
                            int counter = 0,
                            int maxRetry = 3)
        {
            KeyVaultSecret? keyVaultSecret;
            try
            {
                keyVaultSecret = await saveOrDeleteSecret.Invoke(secretKey, secretText).ConfigureAwait(false);
                return (true, keyVaultSecret);
            }
            catch (RequestFailedException failedException) when (failedException.ErrorCode == "Conflict")
            {
                try
                {
                    await _secretClient.PurgeDeletedSecretAsync(secretKey).ConfigureAwait(false);
                }
                catch (Exception) { }
            }
            catch (Exception)
            {
                throw;
            }
            if (counter == maxRetry) 
            {
                return (false, default);
            }
            counter++;
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            return await SaveOrDeleteSecretInKeyVaultWithRetry(secretKey,
                secretText, 
                saveOrDeleteSecret,
                cancellationToken,
                counter, 
                maxRetry).ConfigureAwait(false);
        }

        public Task<(bool wasSaved, KeyVaultKey keyVaultKey)> CreateOrDeleteKeyWithRetry(CreateKeyOptions keyOptions, CancellationToken token, Func<CreateKeyOptions, CancellationToken, Task<KeyVaultKey>> createOrDeleteKey, int counter = 0, int maxRetry = 3)
        {
            throw new NotImplementedException();
        }
    }
}

