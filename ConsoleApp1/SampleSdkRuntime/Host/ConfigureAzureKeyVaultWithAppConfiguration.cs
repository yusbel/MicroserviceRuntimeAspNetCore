using Azure.Data.AppConfiguration;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using Sample.Sdk.Core.Azure.Factory;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Options;
using System.Text.Json;

namespace SampleSdkRuntime.Host
{
    public class ConfigureAzureKeyVaultWithAppConfiguration
    {
        public static void Configure(AzureAppConfigurationKeyVaultOptions appConfigKeyVaultOptions, HostBuilderContext hostCtx, string serviceInstanceName) 
        {
            var appConfigClient = new ConfigurationClient(Environment.GetEnvironmentVariable(ConfigVarConst.APP_CONFIG_CONN_STR));
            var keyVaultConfigAppKey = $"{serviceInstanceName}:{AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION_APP_CONFIG}";
            var serviceVaultUri = appConfigClient.GetConfigurationSetting(
                keyVaultConfigAppKey,
                Environment.GetEnvironmentVariable(ConfigVarConst.ENVIRONMENT_VAR));

            var keyVaultOptions = JsonSerializer.Deserialize<AzureKeyVaultOptions>(serviceVaultUri.Value.Value);

            var tokenClientFactory = new ClientOAuthTokenProviderFactory(hostCtx.Configuration);
            var clientSecretCredential = tokenClientFactory.GetClientSecretCredential();
            var secretClient = new SecretClient(new Uri(keyVaultOptions!.VaultUri), clientSecretCredential);

            appConfigKeyVaultOptions.SetSecretResolver(async keyVaultUri =>
            {
                var secretName = keyVaultUri.AbsolutePath.Substring("/secrets/".Length, keyVaultUri.AbsolutePath.Length - "/secrets/".Length);
                var keyVaultStr = keyVaultUri.ToString().Substring(0, keyVaultUri.ToString().Length - keyVaultUri.AbsolutePath.Length);
                var secretClient = new SecretClient(new Uri(keyVaultStr), clientSecretCredential);
                var secret = await secretClient.GetSecretAsync(secretName).ConfigureAwait(false);
                return secret.Value.Value;
            });
        }
    }
}
