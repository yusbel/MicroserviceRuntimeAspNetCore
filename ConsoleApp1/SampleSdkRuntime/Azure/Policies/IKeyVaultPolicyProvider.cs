using Azure.ResourceManager.KeyVault.Models;
using Microsoft.Graph;

namespace SampleSdkRuntime.Azure.Policies
{
    public interface IKeyVaultPolicyProvider
    {
        Task<(bool wasCreated, KeyVaultAccessPolicyParameters? keyVaultAccessPolicyParameters)>
            CreatePolicy(string tenantId,
            string resourceId,
            Application application,
            ServicePrincipal servicePrincipal,
            CancellationToken cancellationToken);
        Task<bool> DeleteAccessPolicy(
            string tenantId,
            Guid appId,
            Guid servicePrincipalId,
            string resourceIdentifier,
            CancellationToken cancellationToken);
    }
}