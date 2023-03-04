using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Sample.Sdk.Core.Exceptions;
using SampleSdkRuntime.Azure.Factory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.ActiveDirectoryLibs.ServiceAccount
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
                principal = await graph.ServicePrincipals.Request()
                                            .AddAsync(new ServicePrincipal()
                                            {
                                                AppId = identifier,
                                                AppRoleAssignmentRequired = true
                                            }, cancellationToken);
            }
            catch (Exception e)
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
            IGraphServiceServicePrincipalsCollectionPage servicePrincipals = null;
            try
            {
                servicePrincipals = await graph!.ServicePrincipals.Request()
                    .Filter($"appid eq '{identifier}'")
                    .GetAsync(cancellationToken);
                if (servicePrincipals == null || servicePrincipals.Count == 0)
                {
                    return default;
                }
                return servicePrincipals.First();
            }
            catch (Exception e)
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
                await graph!.ServicePrincipals[identifier].Request().DeleteAsync(cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, $"Microsoft graph fail to remove service principal with identifier {identifier}");
            }
            return false;
        }
    }
}
