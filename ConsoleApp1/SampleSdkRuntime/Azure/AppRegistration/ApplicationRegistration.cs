using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Sample.Sdk.Core.Exceptions;
using SampleSdkRuntime.Azure.DataOptions;
using SampleSdkRuntime.Azure.Factory;
using SampleSdkRuntime.Azure.Policies;
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
        private readonly IClientTokenCredentialFactory _clientSecretCredFactory;
        private readonly IGraphServiceClientFactory _graphServiceClientFactory;
        private readonly IKeyVaultPolicyProvider _keyVaultPolicyProvider;
        private readonly SecretClient _secretClient;
        private readonly ILogger<ApplicationRegistration> _logger;
        private readonly RuntimeAzureOptions _azureOptions;

        public ApplicationRegistration(
                    IClientTokenCredentialFactory clientSecretCredFactory,
                    IGraphServiceClientFactory graphServiceClientFactory,
                    IKeyVaultPolicyProvider keyVaultPolicyProvider,
                    SecretClient secretClient,
                    IOptions<RuntimeAzureOptions> azureOptions,
                    ILogger<ApplicationRegistration> logger)
        {
            _clientSecretCredFactory = clientSecretCredFactory;
            _graphServiceClientFactory = graphServiceClientFactory;
            _keyVaultPolicyProvider = keyVaultPolicyProvider;
            _secretClient = secretClient;
            _logger = logger;
            _azureOptions = azureOptions == null ? RuntimeAzureOptions.CreateDefault() : azureOptions.Value;
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
                                    .GetAsync(cancellationToken);
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
                IGraphServiceServicePrincipalsCollectionPage? servicePrincipals = null;
                try
                {
                    servicePrincipals = await graphClient.ServicePrincipals.Request()
                    .Filter($"AppId eq '{application.AppId}'")
                    .GetAsync(cancellationToken);
                }
                catch (RequestFailedException rfe) when (rfe.ErrorCode == "ServicePrincipalNotFound")
                {
                    rfe.LogCriticalException(_logger, $"Service principal for app id {application.Id} not found when deleting the app");
                    continue;
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, $"Exception occurren when deleting app {application.Id}");
                    continue;
                }
                var servicePricipal = servicePrincipals.First();
                //deleting secret from key vault
                try
                {
                    await _secretClient.StartDeleteSecretAsync($"{prefix}-{appIdentifier}");
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Fail to delete the secret from azure key vault");
                }
                //deleting service principal key vault access policy
                try
                {
                    await _keyVaultPolicyProvider.DeleteAccessPolicy(tenantId,
                        new Guid(application.Id),
                        new Guid(servicePricipal.Id),
                        _azureOptions.RuntimeKeyVaultOptions.KeyVaultResourceId,
                        cancellationToken);
                    await Task.Delay(1000);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, $"An error ocurred when deleting the access policy for service account with displayname {appIdentifier}");
                }
                try
                {
                    await graphClient.Applications[application.Id].Request().DeleteAsync(cancellationToken);
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
        public async Task<(bool isValid, Application? app)>
            VerifyApplicationSettings(
            string appIdentifier, 
            CancellationToken cancellationToken, 
            string prefix = "Service")
        {
            var graphClient = _graphServiceClientFactory.Create();
            IGraphServiceApplicationsCollectionPage applications;
            IGraphServiceServicePrincipalsCollectionPage? servicePrincipals = null;
            try
            {
                applications = await graphClient.Applications
                                                .Request()
                                                .Filter($"DisplayName eq '{appIdentifier}'")
                                                .GetAsync(cancellationToken);
                if (applications == null || applications.Count != 1)
                {
                    return (false, default);
                }
                servicePrincipals = await graphClient.ServicePrincipals
                                                        .Request()
                                                        .Filter($"DisplayName eq '{appIdentifier}'")
                                                        .GetAsync(cancellationToken);
                if (servicePrincipals == null || servicePrincipals.Count != 1) 
                {
                    return (false, default);
                }
                var keyVaultSecret = await _secretClient.GetSecretAsync($"{prefix}-{appIdentifier}");
                if (keyVaultSecret == null || keyVaultSecret.Value == null) 
                {
                    return (false, default);
                }
                var serviceRuntimeCredential = _clientSecretCredFactory.GetDefaultCredential();
                var keyPolicyResult = await _keyVaultPolicyProvider.CreatePolicy(
                                                    serviceRuntimeCredential.tenantId,
                                                    _azureOptions.RuntimeKeyVaultOptions.KeyVaultResourceId,
                                                    applications.First(),
                                                    servicePrincipals.First(),
                                                    CancellationToken.None);
                return (true, applications.First());
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

        public async Task<(bool wasSuccess, Application? app)>
            GetOrCreate(string appIdentifier, CancellationToken token, string prefix = "Service")
        {
            var graphClient = _graphServiceClientFactory.Create();
            Application? app = null;
            try
            {
                var apps = await graphClient.Applications
                                    .Request()
                                    .Filter($"DisplayName eq '{appIdentifier}'")
                                    .GetAsync(token);
                app = apps.FirstOrDefault();
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "Application not found");
            }
            ServicePrincipal? servicePrincipal = null;
            if (app == null)
            {
                try
                {
                    app = await graphClient.Applications.Request()
                                                        .AddAsync(new Application()
                                                        {
                                                            DisplayName = appIdentifier
                                                        }, token);
                    await Task.Delay(1000);
                    servicePrincipal = await graphClient.ServicePrincipals.Request().AddAsync(new ServicePrincipal() 
                    {
                        AppId = app.AppId
                    }, token);
                    await Task.Delay(1000);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Application add fail");
                }
                if (app == null || servicePrincipal == null) 
                {
                    _logger.LogCritical($"Service was unable to create service principel andor account, service principal is null {servicePrincipal == null}, application is null {app == null}");
                    return (false, default);
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
                    return (false, default);
                }
                KeyVaultSecret? keyVaultSecret = null;
                DeletedSecret? deletedSecret = null;
                try
                {
                    deletedSecret = await _secretClient.GetDeletedSecretAsync($"{prefix}-{appIdentifier}", token);
                    if (deletedSecret != null)
                    {
                        await _secretClient.PurgeDeletedSecretAsync($"{prefix}-{appIdentifier}", token);
                        await Task.Delay(2000);
                    }
                }
                catch (RequestFailedException rfe) when (rfe.ErrorCode == "SecretNotFound")
                {
                    _logger.LogInformation("Secret not found on the deleted list");
                }
                catch (Exception e) 
                {
                    e.LogCriticalException(_logger, "Error when retrieving deleted items from key vault, adding key vault operation will resume");
                }
                try 
                { 
                    keyVaultSecret = _secretClient.SetSecret(new KeyVaultSecret($"{prefix}-{appIdentifier}", appPass.SecretText), token);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Setting the secret fail");
                    return (false, default);
                }
                try
                {
                    (string tenantId, string clientId, string clientSecret) = _clientSecretCredFactory.GetDefaultCredential();
                    var keyVaultPolicyResult = await _keyVaultPolicyProvider.CreatePolicy(
                        tenantId, 
                        _azureOptions.RuntimeKeyVaultOptions.KeyVaultResourceId, 
                        app,
                        servicePrincipal,
                        token);
                    return (true, app);
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Creating policy has fail");
                    return (false, default);
                }
            }
            return (false, default);
        }
    }
}
