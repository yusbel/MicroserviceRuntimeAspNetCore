﻿using Azure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Interface.Azure.Factory
{
    public interface IClientOAuthTokenProviderFactory
    {
        public bool TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out ClientSecretCredential clientSecretCredential);
        (string TenantId, string ClientId, string ClientSecret)
            GetAzureServiceInstanceCredential();

        (string TenantId, string ClientId, string ClientSecret)
            GetAzureRuntimeServiceCredential();
        string GetDefaultTenantId();

        (string TenantId, string ClientId, string ClientSecret)
            GetAzureTokenCredentials();

        ClientSecretCredential GetClientSecretCredential();
    }
}
