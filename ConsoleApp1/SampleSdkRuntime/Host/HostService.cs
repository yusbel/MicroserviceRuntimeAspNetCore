using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Msg;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Registration;
using Sample.Sdk.Interface;

namespace SampleSdkRuntime.Host
{
    public class HostService
    {
        public static async Task<IHost> Run(IHostBuilder hostBuilder, ServiceRegistration serviceReg, string[] args, CancellationToken token) 
        {
            var serviceHostVariables = new Dictionary<string, string>
                {
                    { ConfigVar.AZURE_TENANT_ID, Environment.GetEnvironmentVariable(ConfigVar.AZURE_TENANT_ID)! },
                    { ConfigVar.AZURE_CLIENT_ID, serviceReg.Credentials.First().ClientId },
                    { ConfigVar.AZURE_CLIENT_SECRET, serviceReg.Credentials.First().ServiceSecretText },
                    { ConfigVar.SERVICE_INSTANCE_NAME_ID, serviceReg.ServiceInstanceId },
                    { ConfigVar.IS_RUNTIME, "false" }
                };

            var host = hostBuilder
                .ConfigureAppConfiguration((host, appConfig) => 
                {
                    appConfig.AddInMemoryCollection(serviceHostVariables);
                    appConfig.AddAzureAppConfiguration(appConfig =>
                    {
                        appConfig.Connect(Environment.GetEnvironmentVariable(ConfigVar.APP_CONFIG_CONN_STR));
                        appConfig.Select(KeyFilter.Any, LabelFilter.Null);
                        appConfig.Select(KeyFilter.Any, Environment.GetEnvironmentVariable(ConfigVar.ENVIRONMENT_VAR));
                        appConfig.ConfigureKeyVault(appConfigKeyVault =>
                        {
                            ConfigureAzureKeyVaultWithAppConfiguration.Configure(appConfigKeyVault, host, args[0]);
                        });
                    });
                })
                .ConfigureServices((host, services) =>
                {
                    services.TryAddSingleton<IServiceContext>(serviceProvider =>
                    {
                        var serviceContext = new ServiceContext(serviceReg);
                        return serviceContext;
                    });
                    services.AddServicesCore(host.Configuration);
                    services.AddInMemoryServices(host.Configuration);
                })
                .Build();
            try
            {
                await host.RunAsync(token);
            }
            catch (Exception)
            {
                throw;
            }
            return host;
        }
    }
}
