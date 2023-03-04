using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Sample.Sdk.Core.Exceptions;
using SampleSdkRuntime.Azure.DataOptions;
using SampleSdkRuntime.Azure.Factory.Interfaces;
using SampleSdkRuntime.Azure.Policies;
using SampleSdkRuntime.Azure.ServiceAccount;
using SampleSdkRuntime.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.AppRegistration
{
    /// <summary>
    /// Service runtime use credentials with permission to create an service identity with permission to create applications 
    /// to access azure native services.
    /// Will use multiple accounts limitted to specific native service.
    /// </summary>
    public class ApplicationRegistration : IApplicationRegistration
    {
        private readonly IClientOAuthTokenProviderFactory _clientSecretCredFactory;
        private readonly IGraphServiceClientFactory _graphServiceClientFactory;
        private readonly IKeyVaultPolicyProvider _keyVaultPolicyProvider;
        private readonly SecretClient _secretClient;
        private readonly ILogger<ApplicationRegistration> _logger;
        private readonly IServicePrincipalProvider _servicePrincipalProvider;
        private readonly RuntimeAzureOptions _azureOptions;

        public ApplicationRegistration(
                    IClientOAuthTokenProviderFactory clientSecretCredFactory,
                    IGraphServiceClientFactory graphServiceClientFactory,
                    IKeyVaultPolicyProvider keyVaultPolicyProvider,
                    SecretClient secretClient,
                    IOptions<RuntimeAzureOptions> azureOptions,
                    ILogger<ApplicationRegistration> logger,
                    IServicePrincipalProvider servicePrincipalProvider)
        {
            _clientSecretCredFactory = clientSecretCredFactory;
            _graphServiceClientFactory = graphServiceClientFactory;
            _keyVaultPolicyProvider = keyVaultPolicyProvider;
            _secretClient = secretClient;
            _logger = logger;
            _servicePrincipalProvider = servicePrincipalProvider;
            _azureOptions = azureOptions == null || azureOptions.Value == null || azureOptions.Value.RuntimeKeyVaultOptions == null 
                                                ? RuntimeAzureOptions.CreateDefault() 
                                                : azureOptions.Value;
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
                                            CancellationToken cancellationToken,
                                            string prefix = "Service")
        {
            var tenantId = _clientSecretCredFactory.GetDefaultTenantId();
            var graphClient = _graphServiceClientFactory.Create();
            IGraphServiceApplicationsCollectionPage? applications = null;
            try 
            {
                applications = await graphClient.Applications
                                    .Request()
                                    .Filter($"DisplayName eq '{appIdentifier}'")
                                    .GetAsync(cancellationToken).ConfigureAwait(false);
            }
            catch(Exception e) 
            {
                e.LogCriticalException(_logger, "Fail to retrieve applications from azure");
                return false;
            }
            if (applications == null) 
            {
                return true;
            }
            foreach(var application in applications)
            {
                ServicePrincipal? servicePricipal = null;
                try
                {
                    servicePricipal = await _servicePrincipalProvider.GetServicePrincipal(application.AppId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Exception raised by microsoft graph");
                }
                //deleting secret from key vault
                try
                {
                    await _secretClient.StartDeleteSecretAsync($"{prefix}-{appIdentifier}").ConfigureAwait(false);
                    await Task.Delay(3000).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Fail to delete the secret from azure key vault");
                }
                try
                {
                    await _secretClient.PurgeDeletedSecretAsync($"{prefix}-{appIdentifier}").ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (e is not RequestFailedException failedException || failedException.ErrorCode != "SecretNotFound") 
                    {
                        e.LogCriticalException(_logger, "An error ocurred");
                    }
                }
                //deleting service principal key vault access policy
                try
                {
                    if (application != null && servicePricipal != null)
                    {
                        await _keyVaultPolicyProvider.DeleteAccessPolicy(tenantId,
                                                            new Guid(application.Id),
                                                            new Guid(servicePricipal!.Id),
                                                            _azureOptions.RuntimeKeyVaultOptions.KeyVaultResourceId,
                                                            cancellationToken).ConfigureAwait(false);
                    }
                    else 
                    {
                        _logger.LogInformation("Deleting application without service principal");
                    }
                    
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, $"An error ocurred when deleting the access policy for service account with displayname {appIdentifier}");
                }
                try
                {
                    await graphClient.Applications[application.Id].Request().DeleteAsync(cancellationToken).ConfigureAwait(false);
                    await _servicePrincipalProvider.DeleteServicePricipal(application.AppId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Application delete fail for app", application.Id);
                    throw new RuntimeOperationException("Application delete failt");
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
            CancellationToken cancellationToken, 
            string prefix = "Service")
        {
            var graphClient = _graphServiceClientFactory.Create();
            IGraphServiceApplicationsCollectionPage applications;
            ServicePrincipal? servicePrincipal = null;
            try
            {
                applications = await graphClient!.Applications
                                                .Request()
                                                .Filter($"DisplayName eq '{appIdentifier}'")
                                                .GetAsync(cancellationToken);
                if (applications == null || applications.Count != 1)
                {
                    return (false, ServiceDependecyStatus.Setup.ApplicationOrServicePrincipleNotFound);//runtime stop the service and recreate the
                }
                try
                {
                    servicePrincipal = await _servicePrincipalProvider.GetServicePrincipal(appIdentifier, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Microsoft graph raise an exception retrieving the service provider");
                    return (false, ServiceDependecyStatus.Setup.ApplicationOrServicePrincipleNotFound);
                }
                var keyVaultSecret = await _secretClient.GetSecretAsync($"{prefix}-{appIdentifier}");
                if (keyVaultSecret == null || keyVaultSecret.Value == null) 
                {
                    return (false, ServiceDependecyStatus.Setup.ApplicationIdSecretNotFoundOnKeyVault);
                }
                var tenantId = _clientSecretCredFactory.GetDefaultTenantId();
                var keyPolicyResult = await _keyVaultPolicyProvider.CreatePolicy(
                                                    tenantId,
                                                    _azureOptions.RuntimeKeyVaultOptions.KeyVaultResourceId,
                                                    applications.First(),
                                                    servicePrincipal!,
                                                    CancellationToken.None);

                return (true, ServiceDependecyStatus.Setup.None);
            }
            catch (Exception e) 
            {
                e.LogCriticalException(_logger, "Failt to read applications from azure active directory using microsoft grpah client");
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

        public async Task<(bool wasSuccess, Application? app, ServicePrincipal? principal, string? clientSecret)>
            DeleteAndCreate(string appIdentifier, CancellationToken token, string prefix = "Service")
        {
            var result = await DeleteAll(appIdentifier, token, prefix);
            var graphClient = _graphServiceClientFactory.Create();
            Application? app = null;
            ServicePrincipal? principal = null;
            try
            {
                app = await graphClient!.Applications
                    .Request()
                    .AddAsync(new Application()
                    {
                        DisplayName = appIdentifier, 
                        SignInAudience = "AzureADMyOrg"
                    }, token);

                principal = await graphClient.ServicePrincipals.Request().AddAsync(new ServicePrincipal() { AppId = app.AppId }, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "Application add fail");
                return (false, default, default, default);
            }
            if (app == null || principal == null) 
            {
                _logger.LogCritical($"Service was unable to create service principel andor account, service principal is null {principal == null}, application is null {app == null}");
                return (false, default, default, default);
            }
            //add password
            PasswordCredential? appPass = null;
            try
            {
                appPass = await graphClient.Applications[app.Id]
                                                            .AddPassword(new PasswordCredential
                                                            {
                                                                DisplayName = $"{prefix}-{appIdentifier}"
                                                            })
                                                            .Request()
                                                            .PostAsync(token);
                    
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "adding password fail");
                return (false, default, default, default);
            }
            KeyVaultSecret? keyVaultSecret;
            try 
            { 
                keyVaultSecret = _secretClient.SetSecret(new KeyVaultSecret($"{prefix}-{appIdentifier}", appPass.SecretText), token);
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "Setting the secret fail");
                return (false, default, default, default);
            }
            try
            {
                (string tenantId, string clientId, string clientSecret) = _clientSecretCredFactory.GetAzureTokenCredentials();
                var keyVaultPolicyResult = await _keyVaultPolicyProvider.CreatePolicy(
                    tenantId, 
                    _azureOptions.RuntimeKeyVaultOptions.KeyVaultResourceId, 
                    app,
                    principal,
                    token);
                return (true, app, principal, keyVaultSecret.Value);
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "Creating policy has fail");
                return (false, default, default, default);
            }
        }

        public async Task<(bool wasFound, Application? app, ServicePrincipal? principal, string? clientSecret)>
            GetApplicationDetails(string appId, CancellationToken token, string prefix = "Service")
        {
            var graphClient = _graphServiceClientFactory.Create();
            Application? app = null;
            try
            {
                var apps = await graphClient!.Applications
                                    .Request()
                                    .Filter($"DisplayName eq '{appId}'")
                                    .GetAsync(token);
                app = apps.FirstOrDefault();
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "Application not found");
                return (false, default, default, default);
            }
            ServicePrincipal? servicePrincipal = null;
            if (app != null)
            {
                try
                {
                    servicePrincipal = await _servicePrincipalProvider.GetServicePrincipal(app.AppId, token);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Fail retrieving service principal");
                    return (false, app, default, default);
                }
            }
            if (app != null && servicePrincipal != null) 
            {
                try
                {
                    var secret = await _secretClient.GetSecretAsync($"{prefix}-{appId}", null, token);
                    return (true, app, servicePrincipal, secret.Value.Value);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Unable to retrieve service principle secret");
                    return (false, app, servicePrincipal, default);
                }
            }
            return (false, default, default, default);
        }
    }
}
