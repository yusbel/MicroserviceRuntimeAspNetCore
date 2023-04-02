using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Interface.Azure.ActiveDirectoryLibs;
using Sample.Sdk.Interface.Azure.Factory;

namespace Sample.Sdk.Core.Azure.ActiveDirectoryLibs.ServiceAccount
{
    public class ServicePrincipalProvider : IServicePrincipalProvider
    {
        private readonly IGraphServiceClientFactory _graphServiceClientFactory;
        private readonly ILogger<ServicePrincipalProvider> _logger;

        public ServicePrincipalProvider(IGraphServiceClientFactory graphServiceClientFactory,
            ILogger<ServicePrincipalProvider> logger)
        {
            _graphServiceClientFactory = graphServiceClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Create service principal for a given app identifier
        /// </summary>
        /// <param name="identifier">Application identifier</param>
        /// <param name="cancellationToken">Cancellation token for this operation</param>
        /// <returns>Service principal when created</returns>
        /// <exception cref="Exception">Raise by microsfot graph sdk</exception>
        public async Task<ServicePrincipal?> Create(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return default;
            }

            var principal = await GetServicePrincipal(identifier, cancellationToken).ConfigureAwait(false);

            if (principal != null)
            {
                return principal;
            }
            var graph = _graphServiceClientFactory.Create();
            if (graph == null) { return default; }
            try
            {
                principal = await graph.ServicePrincipals.PostAsync(new ServicePrincipal()
                {
                    AppId = identifier,
                    AppRoleAssignmentRequired = true
                }, null, cancellationToken);
            }
            catch (Exception)
            {
                throw;
            }

            return principal;
        }

        /// <summary>
        /// Return a service principal giving a application identifier associated with the principal
        /// </summary>
        /// <param name="identifier">application identifier</param>
        /// <param name="cancellationToken">token to cancel the operation</param>
        /// <returns>Service principal</returns>
        /// <exception cref="Exception">Exception throw by the graph sdk</exception>
        public async Task<ServicePrincipal?> GetServicePrincipal(string identifier, CancellationToken cancellationToken)
        {
            var graph = _graphServiceClientFactory.Create();
            ServicePrincipalCollectionResponse? servicePrincipals = null;
            try
            {
                servicePrincipals = await graph!.ServicePrincipals.GetAsync(reqConfig =>
                                                {
                                                    reqConfig.QueryParameters.Filter = $"appid eq '{identifier}'";
                                                }, cancellationToken).ConfigureAwait(false);

                if (servicePrincipals == null || servicePrincipals?.Value?.Count == 0)
                {
                    return default;
                }
                return servicePrincipals?.Value?.First();
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Retireve principal and them delete principal
        /// </summary>
        /// <param name="identifier">Application identifier associated with the service principal</param>
        /// <param name="cancellationToken">Cancellation token for this operation</param>
        /// <returns>True if it was found and removed otherwise is false</returns>
        /// <exception cref="Exception">Exceptionr aised my microsoft graph sdk</exception>
        public async Task<bool> DeleteServicePricipal(string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier)) { return false; }
            var graph = _graphServiceClientFactory.Create();
            var principal = await GetServicePrincipal(identifier, cancellationToken);
            if (principal == null) { return false; }
            try
            {
                await graph!.ServicePrincipals[identifier].DeleteAsync(null, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
            }
            return false;
        }
    }
}
