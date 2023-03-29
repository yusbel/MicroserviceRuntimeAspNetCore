using Azure.Data.AppConfiguration;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Constants;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Core.Enums;
using Sample.Sdk.Core.Security;
using Sample.Sdk.Msg.Data.Options;
using Sample.Sdk.Msg.Providers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Configurations
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
            var id = configuration.GetValue<string>(ConfigurationVariableConstant.SERVICE_INSTANCE_ID);
            return new ServiceConfiguration()
            {
                config = configuration,
                identifier = id.Substring(0, id.IndexOf("-"))
            };
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

        internal void AddDatabaseSettingsOptions(IServiceCollection services) 
        {
            services.AddOptions<DatabaseSettingOptions>()
                .Configure<SecretClient>((dbSettings, secretClient) => 
                {
                    dbSettings.ConnectionString = secretClient.GetSecret(DatabaseSettingOptions.DatabaseSetting).Value.Value;
                });
        }

        internal void AddExternalValidEndpointsOptions(IServiceCollection services)
        {
            services.Configure<List<ExternalValidEndpointOptions>>(
                config.GetSection($"{identifier}:{ExternalValidEndpointOptions.SERVICE_SECURITY_VALD_ENDPOINTS_ID}"));
        }

        internal string GetKeyVaultUri(Enums.AzureKeyVaultOptionsType keyVaultOptionsType) 
        {
            var options = GetAzureKeyVaultOptions();
            return options.First(o => o.Type == keyVaultOptionsType).VaultUri;
        }

        #region Private members

        private List<AzureKeyVaultOptions> GetAzureKeyVaultOptions()
        {
            var options = new List<AzureKeyVaultOptions>
            {
                new AzureKeyVaultOptions{ Type = Enums.AzureKeyVaultOptionsType.Runtime },
                new AzureKeyVaultOptions{ Type = Enums.AzureKeyVaultOptionsType.ServiceInstance }
            };
            config.GetSection($"{identifier}:{AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION_APP_CONFIG}")
                    .Bind(options.First(option => option.Type == Enums.AzureKeyVaultOptionsType.ServiceInstance));
            config.GetSection(AzureKeyVaultOptions.RUNTIME_KEYVAULT_SECTION_APP_CONFIG)
                    .Bind(options.First(option => option.Type == Enums.AzureKeyVaultOptionsType.Runtime));
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
