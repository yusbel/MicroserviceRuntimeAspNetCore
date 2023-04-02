using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Sample.Sdk.Core;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Registration;
using Sample.Sdk.Interface;
using SampleSdkRuntime.HostedServices;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;

namespace SampleSdkRuntime.Host
{
    public class HostRuntime
    {
        public static IHost Create(string[] args, CancellationToken token) 
        {
            var serviceInstance = $"{args[0]}-{args[1]}";
            var keyValuePair = new Dictionary<string, string>()
            {
                { ConfigVarConst.RUNTIME_AZURE_TENANT_ID, "c8656f45-daf5-42c1-9b29-ac27d3e63bf3" },
                { ConfigVarConst.RUNTIME_AZURE_CLIENT_ID, "0f691c02-1c41-4783-b54c-22d921db4e16" },
                { ConfigVarConst.RUNTIME_AZURE_CLIENT_SECRET, "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x" },
                { ConfigVarConst.IS_RUNTIME, "true" },
                { ConfigVarConst.SERVICE_INSTANCE_NAME_ID, serviceInstance }
            };

            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureWebHost(host =>
                                {
                                    host.UseStartup<RuntimeStartUp>();
                                    host.UseKestrel(options =>
                                    {
                                        options.DisableStringReuse = true;
                                        options.ListenLocalhost(5000);
                                    });
                                })
            .ConfigureAppConfiguration((host, config) => 
            {
                config.AddInMemoryCollection(keyValuePair);
                config.AddAzureAppConfiguration(appConfig =>
                {
                    appConfig.Connect(Environment.GetEnvironmentVariable(ConfigVarConst.APP_CONFIG_CONN_STR));
                    appConfig.Select(KeyFilter.Any, LabelFilter.Null);
                    appConfig.Select(KeyFilter.Any, Environment.GetEnvironmentVariable(ConfigVarConst.ENVIRONMENT_VAR));
                    appConfig.ConfigureKeyVault(configureKeyVault =>
                    {
                        ConfigureAzureKeyVaultWithAppConfiguration.Configure(configureKeyVault, host, args[0]);
                    });
                });
            })
            .ConfigureServices((host,services) => 
            {
                services.AddRuntimeServices(host.Configuration, new ServiceContext(ServiceRegistration.DefaultInstance(serviceInstance)));
                services.AddTransient<IServiceContext>(sp =>
                {
                    return new ServiceContext(ServiceRegistration.DefaultInstance(serviceInstance));
                });
                services.AddHostedService<RuntimeSetupHostedAppService>();
            })
            .Build();

            var logger = host.Services.GetRequiredService<ILogger<ServiceRuntime>>();
            try
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await host.RunAsync(token).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        e.LogException(logger.LogCritical);
                    }
                }, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogException(logger.LogCritical);
                throw;
            }

            return host;
        }
    }
}
