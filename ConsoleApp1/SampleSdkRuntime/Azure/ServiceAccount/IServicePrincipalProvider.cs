using Microsoft.Graph;

namespace SampleSdkRuntime.Azure.ServiceAccount
{
    public interface IServicePrincipalProvider
    {
        Task<ServicePrincipal?> Create(string identifier, CancellationToken cancellationToken);
        Task<bool> DeleteServicePricipal(string identifier, CancellationToken cancellationToken);
        Task<ServicePrincipal?> GetServicePrincipal(string identifier, CancellationToken cancellationToken);
    }
}