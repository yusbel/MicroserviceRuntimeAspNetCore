using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Azure.Factory.Interfaces;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using SampleSdkRuntime.AzureAdmin.ActiveDirectoryLibs.AppRegistration;
using SampleSdkRuntime.AzureAdmin.BlobStorageLibs;
using SampleSdkRuntime.AzureAdmin.KeyVaultLibs.Interfaces;
using SampleSdkRuntime.Data;
using SampleSdkRuntime.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sample.Sdk.Core.Enums.Enums;

namespace SampleSdkRuntime.Providers.Registration
{
    internal class ServiceRegistrationProvider : IServiceRegistrationProvider
    {
        private IServiceProvider _serviceProvider = null;
        private readonly IBlobProvider _blobProvider;
        private ServiceRegistration _serviceRegistration = new ServiceRegistration();

        public ServiceRegistrationProvider(IServiceProvider serviceProvider,
            IBlobProvider blobProvider)
        {
            _serviceProvider = serviceProvider;
            _blobProvider = blobProvider;
        }
        internal static ServiceRegistrationProvider Create(IServiceProvider serviceProvider)
        {
            return new ServiceRegistrationProvider(serviceProvider,
                serviceProvider.GetRequiredService<IBlobProvider>());
        }

        public async Task<ServiceRegistration> GetServiceRegistration(string appId, CancellationToken token)
        {
            var appReg = _serviceProvider.GetRequiredService<IApplicationRegistration>();
            AppRegistrationSetup appRegSetup = null;
            try
            {
                appRegSetup = await appReg.GetApplicationDetails(appId, token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return null;
            }
            var serviceReg = ServiceRegistration.DefaultInstance(appId).Assign(appRegSetup);
            if (serviceReg.WasSuccessful) 
            {
                OnSuccess(serviceReg);
            }
            return serviceReg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="token"></param>
        /// <param name="createCredentials"></param>
        /// <returns></returns>
        /// 
        private Func<Task> _privateConfigCredential = null;
        internal ServiceRegistrationProvider 
            ConfigureServiceCredential(string appId,
                CancellationToken token,
                Func<IServiceProvider, CancellationToken, Task<IEnumerable<ServiceCredential>>> createCredentials = null)
        {
            _privateConfigCredential = () => Task.Run(async () =>
            {
                Func<string, CancellationToken, Task<IEnumerable<ServiceCredential>>> credProvider =
                async (appIdentifier, cancellationToken) =>
                {
                    var credentialProvider = _serviceProvider.GetRequiredService<IServiceCredentialProvider>();
                    return await credentialProvider.CreateOrGetCredentials(GetAppDisplayName(appId), token).ConfigureAwait(false);
                };
                _serviceRegistration.Credentials.AddRange(await credProvider.Invoke(GetAppDisplayName(appId), token).ConfigureAwait(false));
                if (createCredentials != null)
                    _serviceRegistration.Credentials.AddRange(await createCredentials.Invoke(_serviceProvider, token).ConfigureAwait(false));
                _serviceRegistration.ServiceInstanceId = appId;
                _serviceRegistration.Credentials.ForEach(credential => 
                { 
                    credential.ServiceSecretKeyCertificateName = GetAppDisplayName(appId);
                    credential.AppIdentifier = GetAppDisplayName(appId);
                });
            });
            return this;
        }

        private string GetAppDisplayName(string appId) 
        {
            return appId.Replace("-", "");
        }

        private Func<Task> _privateConfigServiceCryptoSecret = null;
        internal ServiceRegistrationProvider ConfigureServiceCryptoSecret(CancellationToken token,
            Func<IServiceProvider, CancellationToken, (string, string), Task<IEnumerable<ServiceCryptoSecret>>>
            createOrGetSecrets = null)
        {
            _privateConfigServiceCryptoSecret = () => Task.Run(async () =>
            {
                var tasks = new List<Task>();
                _serviceRegistration.Credentials.Where(item => item.PersistSecretOnKeyVault).ToList()
                    .ForEach(credential =>
                            {
                                if (createOrGetSecrets != null)
                                {
                                    var task = createOrGetSecrets.Invoke(_serviceProvider, token, (credential.ServiceSecretKeyCertificateName, credential.ServiceSecretText))
                                    .ContinueWith(result =>
                                    {
                                        _serviceRegistration.Secrets.AddRange(result.Result);
                                    });
                                    _ = task.ConfigureAwait(false);
                                    tasks.Add(task);
                                };
                                var createTask = createOrGetSecretOnKeyVault(credential, this, token);
                                createTask.ConfigureAwait(false);
                                tasks.Add(createTask);
                            });
                await Task.WhenAll(tasks);
            });
            return this;
        }

        private string GetMessageKey() 
        {
            return $"AesMsgKey{_serviceRegistration.ServiceInstanceId.Replace("-", string.Empty)}";
        }

        private Func<Task> _privateConfigAesCyptoKeyInKeyVault = null;
        internal ServiceRegistrationProvider ConfigureAesCryptoKeyInKeyVault(CancellationToken token) 
        {
            _privateConfigAesCyptoKeyInKeyVault = () => Task.Run(async() => 
            {
                var aesKeyProvider = _serviceProvider.GetRequiredService<IAesKeyRandom>();
                var key = aesKeyProvider.GenerateRandomKey(256);
                var keyVaultProvider = _serviceProvider.GetRequiredService<IKeyVaultProvider>();
                var result = await keyVaultProvider.SaveOrDeleteSecretInKeyVaultWithRetry(GetMessageKey(),
                                    Encoding.UTF8.GetString(key),
                                    async (secretKey, secretText) =>
                                    {
                                        var secretClientFactory = _serviceProvider.GetRequiredService<IAzureClientFactory<SecretClient>>();
                                        var secretClient = secretClientFactory.CreateClient(HostTypeOptions.ServiceInstance.ToString());
                                        var result = await secretClient.SetSecretAsync(secretKey, secretText, token).ConfigureAwait(false);
                                        return result.Value;
                                    },
                                    token)
                                .ConfigureAwait(false);

                _serviceRegistration.Secrets.Add(new ServiceCryptoSecret()
                {
                    SecretId = result!.Secret!.Id.ToString(), 
                    SecretKey = result.Secret.Name, 
                    SecretText = result.Secret.Value
                });
            });
            return this;
        }

        private Func<Task> _privateConfigCryptoKey = null;
        internal ServiceRegistrationProvider 
            ConfigureServiceCryptoKey(CancellationToken token)
        {
            _privateConfigCryptoKey = () => Task.Run((Func<Task?>)(async () =>
            {
                foreach(var credential in _serviceRegistration.Credentials)
                {
                    var keyClient = _serviceProvider.GetRequiredService<KeyClient>();
                    var keyVaultProvider = _serviceProvider.GetRequiredService<IKeyVaultProvider>();
                    var options = new CreateOctKeyOptions(credential.ServiceSecretKeyCertificateName.Replace("-", string.Empty))
                    {
                        Enabled = true,
                        KeySize = 128
                    };
                    try
                    {
                        var factory = _serviceProvider.GetRequiredService<IClientOAuthTokenProviderFactory>();
                        factory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecretCredential);
                        var azureKeyVaultOptions = _serviceProvider.GetRequiredService<IOptions<AzureKeyVaultOptions>>();
                        var managedHsmClient = new KeyClient(new Uri(azureKeyVaultOptions.Value.VaultUri), clientSecretCredential);

                        var octKeyOptions = new CreateOctKeyOptions($"CloudOctKey-{Guid.NewGuid()}")
                        {
                            KeySize = 256,
                        };

                        KeyVaultKey cloudOctKey = managedHsmClient.CreateOctKey(octKeyOptions);

                        var keyVaultResult = await keyVaultProvider.CreateOrDeleteOctKeyWithRetry(options,
                                                                token,
                                                                async (options, token) =>
                                                                {
                                                                    var result = await keyClient.CreateOctKeyAsync(options, token)
                                                                                                .ConfigureAwait(false);
                                                                    return result.Value;
                                                                }).ConfigureAwait(false);
                        _serviceRegistration.Keys.Add(new ServiceCryptoKey()
                        {
                            ServiceKeyId = keyVaultResult.keyVaultKey.Id.ToString(),
                            ServiceKeyName = keyVaultResult.keyVaultKey.Name
                        });
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                };
            }));
            return this;
        }

        internal ServiceRegistration Build()
        {
            _privateConfigCredential().Wait();
            _privateConfigServiceCryptoSecret().Wait();
            UploadCertificatePublicKey("ServiceRuntime:SignatureCertificateName", CancellationToken.None).Wait();

            OnSuccess(_serviceRegistration);
            return _serviceRegistration;
        }

        /// <summary>
        /// Create secret in key vault for credentials
        /// </summary>
        Func<ServiceCredential, ServiceRegistrationProvider, CancellationToken, Task<bool>> createOrGetSecretOnKeyVault =
            async (serviceCred, serviceRegProvider, token) =>
            {
                var keyVaultProvider = serviceRegProvider._serviceProvider.GetRequiredService<IKeyVaultProvider>();
                var secretClientFactory = serviceRegProvider._serviceProvider.GetRequiredService<IAzureClientFactory<SecretClient>>();
                var secretClient = secretClientFactory.CreateClient(HostTypeOptions.ServiceInstance.ToString());
                var keyVaultResult = await keyVaultProvider
                    .SaveOrDeleteSecretInKeyVaultWithRetry(serviceCred.ServiceSecretKeyCertificateName,
                                serviceCred.ServiceSecretText,
                                async (secretKey, secretText) =>
                                {
                                    return await secretClient.SetSecretAsync(secretKey, secretText, token)
                                                                .ConfigureAwait(false);
                                },
                                token);
                serviceRegProvider._serviceRegistration.Secrets.Add(new ServiceCryptoSecret()
                {
                    SecretKey = serviceCred.ServiceSecretKeyCertificateName,
                    SecretText = serviceCred.ServiceSecretText,
                    SecretId = keyVaultResult!.Secret!.Id.ToString()
                });
                return true;
            };
        private string GetConfigValue(string key)
        {
            return _serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>(key);
        }

        private void OnSuccess(ServiceRegistration serviceReg) 
        {
            var aesKeyProvider = _serviceProvider.GetRequiredService<IAesKeyRandom>();
            for (var i = 0; i < 30; i++) 
            {
                serviceReg.AesKeys.Add(aesKeyProvider.GenerateRandomKey(256));
            }
        }

        /// <summary>
        /// "ServiceRuntime:SignatureCertificateName"
        /// </summary>
        /// <param name="appSettingName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> UploadCertificatePublicKey(string appSettingName, 
            CancellationToken token) 
        {
            try
            {
                return await _blobProvider.UploadPublicKey(appSettingName, token)
                                                    .ConfigureAwait(false);

            }
            catch (Exception) 
            {
                throw;
            }
        }
    }
}
