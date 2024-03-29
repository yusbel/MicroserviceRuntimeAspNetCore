﻿using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Interface.Azure.Factory;
using Sample.Sdk.Interface.Azure.KeyVaultLibs;

namespace Sample.Sdk.Core.Azure.KeyVaultLibs
{
    public class KeyVaultPolicyProvider : IKeyVaultPolicyProvider
    {
        private readonly IClientOAuthTokenProviderFactory _clientTokenFactory;
        private readonly ILogger<KeyVaultPolicyProvider> _logger;
        private readonly ArmClient _armClient;

        public KeyVaultPolicyProvider(IArmClientFactory armClientFactory,
                                    IClientOAuthTokenProviderFactory clientTokenFactory,
                                    ILogger<KeyVaultPolicyProvider> logger)
        {
            _armClient = armClientFactory.Create();
            _clientTokenFactory = clientTokenFactory;
            _logger = logger;
        }

        private IEnumerable<IdentityAccessKeyPermission> GetKeyVaultPermissionsKeyForServiceAccount()
        {
            yield return IdentityAccessKeyPermission.Get;
            yield return IdentityAccessKeyPermission.List;
            yield return IdentityAccessKeyPermission.Create;
            yield return IdentityAccessKeyPermission.WrapKey;
            yield return IdentityAccessKeyPermission.UnwrapKey;
            yield return IdentityAccessKeyPermission.Verify;
            yield return IdentityAccessKeyPermission.Delete;
        }

        private IEnumerable<IdentityAccessCertificatePermission> GetKeyVaultPermissionsCertificateForServiceAccount()
        {
            yield return IdentityAccessCertificatePermission.Get;
            yield return IdentityAccessCertificatePermission.List;
            yield return IdentityAccessCertificatePermission.Create;
            yield return IdentityAccessCertificatePermission.Delete;
        }

        private IEnumerable<IdentityAccessSecretPermission> GetKeyVaultPermissionsSecretForServiceAccount()
        {
            yield return IdentityAccessSecretPermission.Get;
            yield return IdentityAccessSecretPermission.List;
            yield return IdentityAccessSecretPermission.Set;
            yield return IdentityAccessSecretPermission.Delete;
        }

        public async Task<bool> DeleteAccessPolicy(
            string tenantId,
            Guid appId,
            Guid servicePrincipalId,
            string resourceIdentifier,
            CancellationToken cancellationToken)
        {
            var keyVaultResource = _armClient.GetKeyVaultResource(new ResourceIdentifier(resourceIdentifier));
            if (keyVaultResource == null)
            {
                return false;
            }
            var permissions = GetAccessPolicyPermissionsForServicePrincipal();
            if (permissions == null)
            {
                return false;
            }
            var accessPolicyParameters = GetKeyVaultAccessPolicyParametersForServicePrincipal(
                                                                                tenantId,
                                                                                appId,
                                                                                servicePrincipalId,
                                                                                permissions);
            try
            {
                await keyVaultResource.UpdateAccessPolicyAsync(AccessPolicyUpdateKind.Remove,
                                                                    accessPolicyParameters,
                                                                    cancellationToken);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return false;
            }
            return true;
        }

        public async Task<(bool wasCreated, KeyVaultAccessPolicyParameters? keyVaultAccessPolicyParameters)>
            CreatePolicy(string tenantId,
                        string resourceId,
                        Application application,
                        ServicePrincipal servicePrincipal,
                        CancellationToken cancellationToken)
        {
            IdentityAccessPermissions permissions = GetAccessPolicyPermissionsForServicePrincipal();
            KeyVaultResource keyVaultResource;
            if (_clientTokenFactory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecretCredential))
            {
                await Task.Delay(1000);
                keyVaultResource = _armClient.GetKeyVaultResource(new ResourceIdentifier(resourceId));
                KeyVaultAccessPolicyParameters keyVaultAccessPolicyParameters = GetKeyVaultAccessPolicyParametersForServicePrincipal(tenantId,
                    new Guid(application.AppId),
                    new Guid(servicePrincipal.Id),
                    permissions);

                //Update access policy
                var keyVaultAccessParameters = await keyVaultResource.UpdateAccessPolicyAsync(AccessPolicyUpdateKind.Add,
                                                            keyVaultAccessPolicyParameters,
                                                            cancellationToken);
                return (true, keyVaultAccessParameters);
            }
            return (false, default);
        }

        private IdentityAccessPermissions GetAccessPolicyPermissionsForServicePrincipal()
        {
            IdentityAccessPermissions permissions = new();
            GetKeyVaultPermissionsKeyForServiceAccount().ToList().ForEach(permission =>
            {
                permissions.Keys.Add(permission);
            });
            GetKeyVaultPermissionsCertificateForServiceAccount().ToList().ForEach(certificate =>
            {
                permissions.Certificates.Add(certificate);
            });
            GetKeyVaultPermissionsSecretForServiceAccount().ToList().ForEach(secret =>
            {
                permissions.Secrets.Add(secret);
            });
            return permissions;
        }

        private static KeyVaultAccessPolicyParameters GetKeyVaultAccessPolicyParametersForServicePrincipal(string tenantId, Guid appId, Guid servicePrincipalId, IdentityAccessPermissions permissions)
        {
            //create keyvault access policies
            var policies = new List<KeyVaultAccessPolicy>()
                                    {
                                        {
                                            new KeyVaultAccessPolicy(new Guid(tenantId), servicePrincipalId.ToString(), permissions)
                                            {
                                                //ApplicationId = appId, 
                                                ObjectId = servicePrincipalId.ToString()
                                            }
                                        }
                                    };
            //Create keyvault access policy properties
            var keyVaultAccessProperties = new KeyVaultAccessPolicyProperties(policies);

            //Create key vault access policy parameters
            var keyVaultAccessPolicyParameters = new KeyVaultAccessPolicyParameters(keyVaultAccessProperties);
            return keyVaultAccessPolicyParameters;
        }
    }
}
