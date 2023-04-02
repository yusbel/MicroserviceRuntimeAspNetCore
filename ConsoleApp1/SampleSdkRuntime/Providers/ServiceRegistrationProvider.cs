using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Data.Azure;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Registration;
using Sample.Sdk.Interface.Azure.ActiveDirectoryLibs;
using Sample.Sdk.Interface.Azure.BlobLibs;
using Sample.Sdk.Interface.Azure.KeyVaultLibs;
using Sample.Sdk.Interface.Registration;
using Sample.Sdk.Interface.Security.Symetric;
using static Sample.Sdk.Data.Enums.Enums;

namespace SampleSdkRuntime.Providers
{
    internal class ServiceRegistrationProvider : IServiceRegistrationProvider
    {
        private IServiceProvider _serviceProvider = null;
        private readonly IBlobProvider _blobProvider;
        private readonly IAesKeyRandom _aesKeyRandom;
        private readonly IConfiguration _config;
        private readonly IApplicationRegistration _applicationRegistration;
        private readonly IServicePrincipalProvider _servicePrincipalProvider;
        private readonly SecretClient _secretClient;
        private ServiceRegistration _serviceRegistration = new ServiceRegistration();
        private string _appId = string.Empty;
        private ServiceRegistrationProvider(string appId,
            IServiceProvider serviceProvider,
            IBlobProvider blobProvider,
            IAesKeyRandom aesKeyRandom,
            IConfiguration config,
            IApplicationRegistration applicationRegistration,
            IServicePrincipalProvider servicePrincipalProvider,
            IAzureClientFactory<SecretClient> azureClientFactory)
        {
            _appId = appId;
            _secretClient = azureClientFactory.CreateClient(HostTypeOptions.ServiceInstance.ToString());
            _serviceProvider = serviceProvider;
            _blobProvider = blobProvider;
            _aesKeyRandom = aesKeyRandom;
            _config = config;
            _applicationRegistration = applicationRegistration;
            _servicePrincipalProvider = servicePrincipalProvider;
        }
        internal static ServiceRegistrationProvider Create(IServiceProvider serviceProvider, string appId)
        {
            return new ServiceRegistrationProvider(appId,
                serviceProvider,
                serviceProvider.GetRequiredService<IBlobProvider>(),
                serviceProvider.GetRequiredService<IAesKeyRandom>(),
                serviceProvider.GetRequiredService<IConfiguration>(),
                serviceProvider.GetRequiredService<IApplicationRegistration>(),
                serviceProvider.GetRequiredService<IServicePrincipalProvider>(),
                serviceProvider.GetRequiredService<IAzureClientFactory<SecretClient>>());
        }

        public async Task<(bool isValid, ServiceRegistration reg)> GetServiceRegistration(string appId, CancellationToken token)
        {
            try
            {
                //retrieving application
                var app = await _applicationRegistration.GetApplication(appId, token)
                                                                        .ConfigureAwait(false);
                if (app == null || app.AppId == null)
                    return (false, new ServiceRegistration());
                //retrieving service principal
                var servicePrincipal = await _servicePrincipalProvider.GetServicePrincipal(app.AppId, token)
                                                                        .ConfigureAwait(false);
                if (servicePrincipal == null)
                    return (false, new ServiceRegistration());
                //retrieve service pricipal password
                var servicePass = await _secretClient.GetSecretAsync(GetAppDisplayName(appId), null, token)
                                                                        .ConfigureAwait(false);
                if (servicePass == null || servicePass.Value == null || servicePass.Value.Value == null)
                    return (false, new ServiceRegistration());
                //retrieve all the public keys used to verify message signature.

                var appRegSetup = AppRegistrationSetup.Create(true, app, servicePrincipal, servicePass.Value.Value);
                var serviceRegistration = ServiceRegistration.DefaultInstance(appId).Assign(appRegSetup);
                return (true, serviceRegistration);
            }
            catch (Exception)
            {
                //no need to log this is expected
                return (false, default);
            }
        }

        /// <summary>
        /// Create service credentials
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="token"></param>
        /// <param name="createCredentials"></param>
        /// <returns></returns>
        /// 
        private Func<Task> _privateConfigCredential = null;
        internal ServiceRegistrationProvider
            ConfigureServiceCredential(CancellationToken token,
                Func<IServiceProvider, CancellationToken, Task<IEnumerable<ServiceCredential>>> createCredentials = null)
        {
            _privateConfigCredential = () => Task.Run(async () =>
            {
                var credentialProvider = _serviceProvider.GetRequiredService<IServiceCredentialProvider>();
                var credentials = await credentialProvider.CreateCredentials(GetAppDisplayName(_appId), token).ConfigureAwait(false);

                _serviceRegistration.Credentials.AddRange(credentials);
                if (createCredentials != null)
                    _serviceRegistration.Credentials.AddRange(await createCredentials.Invoke(_serviceProvider, token).ConfigureAwait(false));
                _serviceRegistration.ServiceInstanceId = _appId;
                _serviceRegistration.Credentials.ForEach(credential =>
                {
                    credential.ServiceSecretKeyCertificateName = GetAppDisplayName(_appId);
                    credential.AppIdentifier = GetAppDisplayName(_appId);
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

        internal async Task<ServiceRegistration> Build(CancellationToken token)
        {
            (bool isValid, _serviceRegistration) = await GetServiceRegistration(_appId, token).ConfigureAwait(false);
            if (!isValid)
            {
                await _privateConfigCredential.Invoke().ConfigureAwait(false);
                await _privateConfigServiceCryptoSecret.Invoke().ConfigureAwait(false);
            }
            var appSettingName = Environment.GetEnvironmentVariable(ConfigVarConst.SERVICE_RUNTIME_CERTIFICATE_NAME_APP_CONFIG_KEY);
            await _blobProvider.UploadSignaturePublicKey(appSettingName!, token)
                                                    .ConfigureAwait(false);

            AssignServiceRegistration(_serviceRegistration);
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

        private void AssignServiceRegistration(ServiceRegistration serviceReg)
        {
            AssignAesKeys(serviceReg);
        }
        private void AssignAesKeys(ServiceRegistration serviceReg)
        {
            for (var i = 0; i < 30; i++)
            {
                serviceReg.AesKeys.Add(_aesKeyRandom.GenerateRandomKey(256));
            }
        }
    }
}
