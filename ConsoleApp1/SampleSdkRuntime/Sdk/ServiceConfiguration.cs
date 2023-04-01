using Azure.Data.AppConfiguration;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Options;
using Sample.Sdk.Msg.Providers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sample.Sdk.Data.Enums.Enums;

namespace SampleSdkRuntime.Sdk
{
    internal class ServiceConfiguration
    {
        private string identifier = string.Empty;
        private IConfiguration config;
        /// <summary>
        /// Connection string will be on the service context created by the service runtime
        /// </summary>
        internal static ServiceConfiguration Create(IConfiguration configuration)
        {
            var id = Environment.GetEnvironmentVariable(ConfigVarConst.SERVICE_INSTANCE_NAME_ID);
            return new ServiceConfiguration()
            {
                config = configuration,
                identifier = id.Substring(0, id.IndexOf("-"))
            };
        }
        internal void AddRuntimeAzureKeyVaultOptions(IServiceCollection services) 
        {
            services.Configure<AzureKeyVaultOptions>(config.GetSection(AzureKeyVaultOptions.RUNTIME_KEYVAULT_SECTION_APP_CONFIG));
        }

        internal void AddAzureKeyVaultOptions(IServiceCollection services)
        {
            services.Configure<List<AzureKeyVaultOptions>>(option =>
            {
                option.AddRange(GetAzureKeyVaultOptions());
            });
        }

        internal void AddCustomProtocolOptions(IServiceCollection services)
        {
            services.Configure<CustomProtocolOptions>(config.GetSection($"{identifier}:{CustomProtocolOptions.Identifier}"));
        }

        internal void AddAzureServiceBusOptions(IServiceCollection services)
        {
            services.Configure<List<AzureMessageSettingsOptions>>(options =>
            {
                options.AddRange(GetServiceBusReceiverOptions());
                options.AddRange(GetServiceBusSenderOptions());
            });
        }

        internal (string senderConnStr, string receiverConnStr) GetServiceBusConnStr()
        {
            return (GetServiceBusSenderOptions().First().ConnStr, GetServiceBusReceiverOptions().First().ConnStr);
        }

        internal ValueTask AddBlobStorageOptions(IServiceCollection services, CancellationToken token)
        {
            services.AddOptions<BlobStorageOptions>()
                .Configure<IAzureClientFactory<SecretClient>>(async (blobOptions, secretClientFactory) =>
                {
                    var secretCient = secretClientFactory.CreateClient(HostTypeOptions.ServiceInstance.ToString());
                    config.GetSection(BlobStorageOptions.Identifier).Bind(blobOptions);
                    var blobConnStr = await secretCient.GetSecretAsync(blobOptions.EmployeeServiceMsgSignatureSecret, null, token).ConfigureAwait(false);
                    blobOptions.BlobConnStr = blobConnStr.Value.Value;
                });

            return ValueTask.CompletedTask;
        }

        internal BlobStorageOptions GetBlobSecretKeyOptions()
        {
            var blobSecretOption = new BlobStorageOptions();
            config.GetSection(BlobStorageOptions.Identifier).Bind(blobSecretOption);
            return blobSecretOption;
        }

        internal void AddDatabaseSettingsOptions(IServiceCollection services)
        {
            services.AddOptions<DatabaseSettingOptions>()
                .Configure<IAzureClientFactory<SecretClient>>((dbSettings, secretClientFactory) =>
                {
                    var secretClient = secretClientFactory.CreateClient(HostTypeOptions.ServiceInstance.ToString());
                    dbSettings.ConnectionString = secretClient.GetSecret(DatabaseSettingOptions.DatabaseSetting).Value.Value;
                });
        }

        internal void AddExternalValidEndpointsOptions(IServiceCollection services)
        {
            services.Configure<List<ExternalValidEndpointOptions>>(
                config.GetSection($"{identifier}:{ExternalValidEndpointOptions.SERVICE_SECURITY_VALD_ENDPOINTS_ID}"));
        }

        internal string GetKeyVaultUri(HostTypeOptions keyVaultOptionsType)
        {
            var options = GetAzureKeyVaultOptions();
            return options.First(o => o.Type == keyVaultOptionsType).VaultUri;
        }

        #region Private members

        private List<AzureKeyVaultOptions> GetAzureKeyVaultOptions()
        {
            var options = new List<AzureKeyVaultOptions>
            {
                new AzureKeyVaultOptions{ Type = HostTypeOptions.Runtime },
                new AzureKeyVaultOptions{ Type = HostTypeOptions.ServiceInstance }
            };
            config.GetSection($"{identifier}:{AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION_APP_CONFIG}")
                    .Bind(options.First(option => option.Type == HostTypeOptions.ServiceInstance));
            config.GetSection(AzureKeyVaultOptions.RUNTIME_KEYVAULT_SECTION_APP_CONFIG)
                    .Bind(options.First(option => option.Type == HostTypeOptions.Runtime));
            return options;
        }

        private List<AzureMessageSettingsOptions> GetServiceBusReceiverOptions()
        {
            var receiverOptions = new List<AzureMessageSettingsOptions>();
            config.GetSection($"{identifier}:{AzureMessageSettingsOptions.RECEIVER_SECTION_ID}")
                        .Bind(receiverOptions);
            return receiverOptions;
        }
        private List<AzureMessageSettingsOptions> GetServiceBusSenderOptions()
        {
            var senderOptions = new List<AzureMessageSettingsOptions>();
            config.GetSection($"{identifier}:{AzureMessageSettingsOptions.SENDER_SECTION_ID}")
                        .Bind(senderOptions);
            return senderOptions;
        }
        #endregion
    }
}
