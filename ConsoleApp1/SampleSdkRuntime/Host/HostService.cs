using Microsoft.Extensions.DependencyInjection.Extensions;
using Sample.Sdk.Core;
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
                    { ConfigVarConst.AZURE_TENANT_ID, Environment.GetEnvironmentVariable(ConfigVarConst.AZURE_TENANT_ID)! },
                    { ConfigVarConst.AZURE_CLIENT_ID, serviceReg.Credentials.First().ClientId },
                    { ConfigVarConst.AZURE_CLIENT_SECRET, serviceReg.Credentials.First().ServiceSecretText },
                    { ConfigVarConst.SERVICE_INSTANCE_NAME_ID, serviceReg.ServiceInstanceId },
                    { ConfigVarConst.IS_RUNTIME, "false" }
                };

            var host = hostBuilder
                .ConfigureAppConfiguration((host, appConfig) => 
                {
                    appConfig.AddInMemoryCollection(serviceHostVariables);
                    appConfig.AddAzureAppConfiguration(appConfig =>
                    {
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
