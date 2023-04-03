using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Options;
using static Sample.Sdk.Data.Enums.Enums;

namespace Sample.Sdk.Core
{
    public class ServiceConfiguration
    {
        private string serviceInstanceName = string.Empty;
        private IConfiguration config;
        /// <summary>
        /// Connection string will be on the service context created by the service runtime
        /// </summary>
        public static ServiceConfiguration Create(IConfiguration configuration)
        {
            var id = Environment.GetEnvironmentVariable(ConfigVar.SERVICE_INSTANCE_NAME_ID);
            return new ServiceConfiguration()
            {
                config = configuration,
                serviceInstanceName = id!.Substring(0, id.IndexOf("-"))
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
            services.Configure<CustomProtocolOptions>(config.GetSection($"{serviceInstanceName}:{CustomProtocolOptions.Identifier}"));
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
            var key = $"{serviceInstanceName}:{DatabaseSettingOptions.DatabaseSetting}";
            services.Configure<DatabaseSettingOptions>(option => option.ConnectionString = config.GetValue<string>(key));
        }

        internal void AddExternalValidEndpointsOptions(IServiceCollection services)
        {
            services.Configure<List<ExternalValidEndpointOptions>>(
                config.GetSection($"{serviceInstanceName}:{ExternalValidEndpointOptions.SERVICE_SECURITY_VALD_ENDPOINTS_ID}"));
        }

        public string GetKeyVaultUri(HostTypeOptions keyVaultOptionsType)
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
            var sectionId = $"{serviceInstanceName}:{AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION_APP_CONFIG}";
            config.GetSection(sectionId)
                    .Bind(options.First(option => option.Type == HostTypeOptions.ServiceInstance));
            config.GetSection(AzureKeyVaultOptions.RUNTIME_KEYVAULT_SECTION_APP_CONFIG)
                    .Bind(options.First(option => option.Type == HostTypeOptions.Runtime));
            return options;
        }

        private List<AzureMessageSettingsOptions> GetServiceBusReceiverOptions()
        {
            var receiverOptions = new List<AzureMessageSettingsOptions>();
            var key = $"{serviceInstanceName}:{AzureMessageSettingsOptions.RECEIVER_SECTION_ID}";
            config.GetSection(key)
                        .Bind(receiverOptions);
            return receiverOptions;
        }
        private List<AzureMessageSettingsOptions> GetServiceBusSenderOptions()
        {
            var senderOptions = new List<AzureMessageSettingsOptions>();
            config.GetSection($"{serviceInstanceName}:{AzureMessageSettingsOptions.SENDER_SECTION_ID}")
                        .Bind(senderOptions);
            return senderOptions;
        }
        #endregion
    }
}
