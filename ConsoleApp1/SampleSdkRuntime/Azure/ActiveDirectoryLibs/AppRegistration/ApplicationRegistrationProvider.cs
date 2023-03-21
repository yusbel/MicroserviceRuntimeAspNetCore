using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Models;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Azure.Factory.Interfaces;
using Sample.Sdk.Core.Exceptions;
using SampleSdkRuntime.Azure.ActiveDirectoryLibs.ServiceAccount;
using SampleSdkRuntime.Azure.KeyVaultLibs.Interfaces;
using SampleSdkRuntime.Exceptions;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.ActiveDirectoryLibs.AppRegistration
{
    /// <summary>
    /// Service runtime use credentials with permission to create an service identity with permission to create applications 
    /// to access azure native services.
    /// Will use multiple accounts limitted to specific native service.
    /// </summary>
    internal class ApplicationRegistrationProvider : IApplicationRegistration
    {
        private readonly IClientOAuthTokenProviderFactory _clientSecretCredFactory;
        private readonly IGraphServiceClientFactory _graphServiceClientFactory;
        private readonly SecretClient _secretClient;
        private readonly ILogger<ApplicationRegistrationProvider> _logger;
        private readonly IServicePrincipalProvider _servicePrincipalProvider;
        private readonly IKeyVaultProvider _keyVaultProvider;
        private readonly KeyClient _keyClient;
        private readonly IOptions<AzureKeyVaultOptions> _azureKeyVaultOptions;

        public ApplicationRegistrationProvider(
                    IClientOAuthTokenProviderFactory clientSecretCredFactory,
                    IGraphServiceClientFactory graphServiceClientFactory,
                    SecretClient secretClient,
                    IOptions<AzureKeyVaultOptions> azureKeyVaultOptions,
                    ILogger<ApplicationRegistrationProvider> logger,
                    IServicePrincipalProvider servicePrincipalProvider,
                    IKeyVaultProvider keyVaultProvider, 
                    KeyClient keyClient)
        {
            _clientSecretCredFactory = clientSecretCredFactory;
            _graphServiceClientFactory = graphServiceClientFactory;
            _secretClient = secretClient;
            _logger = logger;
            _servicePrincipalProvider = servicePrincipalProvider;
            _keyVaultProvider = keyVaultProvider;
            _keyClient = keyClient;
            _azureKeyVaultOptions = azureKeyVaultOptions;
        }
        /// <summary>
        /// Used for operations from the command cli
        /// </summary>
        /// <param name="appIdentifier"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        /// <exception cref="RuntimeOperationException"></exception>
        public async Task<bool> DeleteAll(string appIdentifier,
                                            CancellationToken cancellationToken)
        {
            var tenantId = _clientSecretCredFactory.GetDefaultTenantId();
            var graphClient = _graphServiceClientFactory.Create();
            ApplicationCollectionResponse? applications = null;
            try
            {
                applications = await graphClient.Applications.GetAsync(requestConfig => 
                                            {
                                                requestConfig.QueryParameters.Filter = $"DisplayName eq '{appIdentifier}'";
                                            }, cancellationToken)
                                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return false;
            }
            if (applications == null)
            {
                return true;
            }
            foreach (var application in applications.Value)
            {
                ServicePrincipal? servicePricipal = null;
                try
                {
                    servicePricipal = await _servicePrincipalProvider.GetServicePrincipal(application.AppId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
                }
                //deleting secret from key vault
                try
                {
                    await _secretClient.StartDeleteSecretAsync(GetSecretName(appIdentifier)).ConfigureAwait(false);
                    await Task.Delay(3000).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
                }
                try
                {
                    await _secretClient.PurgeDeletedSecretAsync(GetSecretName(appIdentifier)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (e is not RequestFailedException failedException || failedException.ErrorCode != "SecretNotFound")
                    {
                        e.LogException(_logger.LogCritical);
                    }
                }
                //deleting service principal key vault access policy
                try
                {
                    if (application != null && servicePricipal != null)
                    {
                        await _keyVaultProvider.DeleteAccessPolicy(tenantId,
                                                            new Guid(application.Id),
                                                            new Guid(servicePricipal!.Id),
                                                            _azureKeyVaultOptions.Value.ResourceId,
                                                            cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.LogInformation("Deleting application without service principal");
                    }

                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
                }
                try
                {
                    await graphClient.Applications[application.Id].DeleteAsync(null, cancellationToken).ConfigureAwait(false);
                    await _servicePrincipalProvider.DeleteServicePricipal(application.AppId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
                    throw new RuntimeOperationException("Application delete fail");
                }
            }
            return true;
        }

        /// <summary>
        /// Use by the runtime on an schedule to verify that the service setting are setup properly on azure
        /// </summary>
        /// <param name="appIdentifier"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public async Task<(bool isValid, ServiceDependecyStatus.Setup reason)>
            VerifyApplicationSettings(
            string appIdentifier,
            CancellationToken cancellationToken)
        {
            var graphClient = _graphServiceClientFactory.Create();
            ApplicationCollectionResponse? applications;
            ServicePrincipal? servicePrincipal = null;
            try
            {
                applications = await graphClient!.Applications.GetAsync(reqConf => 
                                            {
                                                reqConf.QueryParameters.Filter = $"DisplayName eq '{appIdentifier}'";
                                            }, cancellationToken).ConfigureAwait(false);
                if (applications == null || applications?.Value?.Count != 1)
                {
                    return (false, ServiceDependecyStatus.Setup.ApplicationOrServicePrincipleNotFound);//runtime stop the service and recreate the
                }
                try
                {
                    servicePrincipal = await _servicePrincipalProvider.GetServicePrincipal(appIdentifier, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
                    return (false, ServiceDependecyStatus.Setup.ApplicationOrServicePrincipleNotFound);
                }
                var keyVaultSecret = await _secretClient.GetSecretAsync(GetSecretName(appIdentifier));
                if (keyVaultSecret == null || keyVaultSecret.Value == null)
                {
                    return (false, ServiceDependecyStatus.Setup.ApplicationIdSecretNotFoundOnKeyVault);
                }
                var tenantId = _clientSecretCredFactory.GetDefaultTenantId();
                var keyPolicyResult = await _keyVaultProvider.CreatePolicy(
                                                    tenantId,
                                                    _azureKeyVaultOptions.Value.ResourceId,
                                                    applications.Value.First(),
                                                    servicePrincipal!,
                                                    CancellationToken.None);

                return (true, ServiceDependecyStatus.Setup.None);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default);
            }
        }

        /// <summary>
        /// Use by the runtime to create the application principles the service use to access azure native services
        /// </summary>
        /// <param name="appIdentifier"></param>
        /// <param name="token"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public async Task<AppRegistrationSetup>
            DeleteAndCreate(string appIdentifier, 
                            CancellationToken token)
        {
            var result = await DeleteAll(appIdentifier, token);
            var graphClient = _graphServiceClientFactory.Create();
            Application? app = null;
            ServicePrincipal? principal = null;
            try
            {
                app = await graphClient!.Applications.PostAsync(new Application()
                                                        {
                                                            DisplayName = GetAppDisplayName(appIdentifier),
                                                            SignInAudience = "AzureADMyOrg"
                                                        }, null, token);

                principal = await _servicePrincipalProvider.Create(app!.AppId!, token);
                if (app == null || principal == null)
                {
                    throw new InvalidOperationException($"{nameof(app)} or {nameof(principal)}");
                }
                var appPass = await AddPassword(app.Id, GetAppDisplayName(appIdentifier), token);

                (string tenantId, string clientId, string clientSecret) = _clientSecretCredFactory.GetAzureTokenCredentials();
                var keyVaultPolicyResult = await _keyVaultProvider.CreatePolicy(tenantId,
                                                            _azureKeyVaultOptions.Value.ResourceId,
                                                            app,
                                                            principal,
                                                            token);

                return AppRegistrationSetup.Create(true,
                                            app,
                                            principal, 
                                            appPass!.SecretText!);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return AppRegistrationSetup.Create(false, default, default, string.Empty);
            }
        }

        private async Task<PasswordCredential> AddPassword(string? id, string displayName, CancellationToken token)
        {
            try
            {
                var graphClient = _graphServiceClientFactory.Create();
                var appPass = await graphClient.Applications[id].AddPassword.PostAsync(new AddPasswordPostRequestBody()
                {
                    PasswordCredential = new PasswordCredential()
                    {
                        DisplayName = displayName
                    }
                }, null, token)
                    .ConfigureAwait(false);
                if (appPass == null)
                {
                    OnErrorAppRegistration(id!, this, token);
                    throw new ArgumentNullException(nameof(appPass));
                }
                return appPass;
            }
            catch (Exception)
            {
                throw;
            }
        }

        Action<string, ApplicationRegistrationProvider, CancellationToken> 
            OnErrorAppRegistration = async (id, appReg, token) => 
        {
            try 
            {
                await appReg.DeleteAll(id, token).ConfigureAwait(false);
            }
            catch(Exception e) 
            {
                e.LogException(appReg._logger.LogCritical);
            }
        };

        private static string GetAppDisplayName(string appIdentifier)
        {
            return appIdentifier.Replace("-", "");
        }

        public async Task<AppRegistrationSetup> 
            GetApplicationDetails(string appId, 
                    CancellationToken token)
        {
            var graphClient = _graphServiceClientFactory.Create();
            Application? app = null;
            try
            {
                var apps = await graphClient!.Applications.GetAsync(reqConfig => 
                                {
                                    reqConfig.QueryParameters.Filter = $"DisplayName eq '{GetAppDisplayName(appId)}'";
                                }, token).ConfigureAwait(false);
                if (apps == null || apps.Value.Count == 0) 
                {
                    return null;
                }
                app = apps?.Value?.FirstOrDefault();
                
                var servicePrincipal = await _servicePrincipalProvider.GetServicePrincipal(app.AppId, token);

                if (app != null && servicePrincipal != null)
                {
                    var clientSecret = await _secretClient.GetSecretAsync(GetAppDisplayName(appId), null, token).ConfigureAwait(false);
                    return AppRegistrationSetup.Create(true, app, servicePrincipal, clientSecret.Value.Value);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return AppRegistrationSetup.Create(false, default, default, default);
        }

        private static string GetSecretName(string appId)
        {
            return appId.Replace("-", "");
        }

        private static string GetKeyName(string appId, string prefix)
        {
            return $"{prefix}{appId}".Replace("-", string.Empty).Replace("_", string.Empty);
        }
    }
}
