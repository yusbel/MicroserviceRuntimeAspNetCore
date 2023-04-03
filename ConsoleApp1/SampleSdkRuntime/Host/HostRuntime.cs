using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Graph.Models;
using Sample.Sdk.Core;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Options;
using Sample.Sdk.Data.Registration;
using Sample.Sdk.Interface;
using SampleSdkRuntime.HostedServices;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Serilog.Ui.MsSqlServerProvider;
using Serilog.Ui.Web;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;

namespace SampleSdkRuntime.Host
{
    public class HostRuntime
    {
        public static IHost Create(string[] args, CancellationToken token) 
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            var serviceInstance = $"{args[0]}-{args[1]}";
            var keyValuePair = new Dictionary<string, string>()
            {
                { ConfigVar.RUNTIME_AZURE_TENANT_ID, "c8656f45-daf5-42c1-9b29-ac27d3e63bf3" },
                { ConfigVar.RUNTIME_AZURE_CLIENT_ID, "0f691c02-1c41-4783-b54c-22d921db4e16" },
                { ConfigVar.RUNTIME_AZURE_CLIENT_SECRET, "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x" },
                { ConfigVar.IS_RUNTIME, "true" },
                { ConfigVar.SERVICE_INSTANCE_NAME_ID, serviceInstance }
            };

            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseSerilog((host,services, config) => 
            {
                config.ReadFrom.Configuration(host.Configuration);
                if (Environment.GetEnvironmentVariable(ConfigVar.ENVIRONMENT_VAR) == "Development") 
                {
                    var serviceCtx = services.GetRequiredService<IServiceContext>();
                    var dbSettingsSectionId = $"{serviceCtx.GetServiceInstanceName()}:{Environment.GetEnvironmentVariable(ConfigVar.DB_CONN_STR)}";
                    var connStr = host.Configuration.GetValue<string>(dbSettingsSectionId);
                    config.WriteTo.MSSqlServer(connStr,
                            sinkOptions: new MSSqlServerSinkOptions()
                            {
                                TableName = "Logs"
                            });
                }
                
            })
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
                    appConfig.Connect(Environment.GetEnvironmentVariable(ConfigVar.APP_CONFIG_CONN_STR));
                    appConfig.Select(KeyFilter.Any, LabelFilter.Null);
                    appConfig.Select(KeyFilter.Any, Environment.GetEnvironmentVariable(ConfigVar.ENVIRONMENT_VAR));
                    appConfig.ConfigureKeyVault(configureKeyVault =>
                    {
                        ConfigureAzureKeyVaultWithAppConfiguration.Configure(configureKeyVault, host, args[0]);
                    });
                });
            })
            .ConfigureServices((host,services) => 
            {
                services.AddSerilogUi(optionBuilder => 
                {
                    var serviceInstanceName = Environment.GetEnvironmentVariable(ConfigVar.SERVICE_INSTANCE_NAME_ID)!.Substring(0, Environment.GetEnvironmentVariable(ConfigVar.SERVICE_INSTANCE_NAME_ID)!.IndexOf("-"));
                    var connStr = host.Configuration.GetValue<string>($"{serviceInstanceName}:{Environment.GetEnvironmentVariable(ConfigVar.DB_CONN_STR)}");
                    optionBuilder.UseSqlServer(connStr, "Logs");
                });
                services.AddRuntimeServices(host.Configuration, new ServiceContext(ServiceRegistration.DefaultInstance(serviceInstance)));
                services.AddTransient<IServiceContext>(sp =>
                {
                    return new ServiceContext(ServiceRegistration.DefaultInstance(serviceInstance));
                });
                services.AddHostedService<RuntimeSetupHostedAppService>();
            })
            .Build();

            var appLifeTime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            appLifeTime.ApplicationStopped.Register(() =>
            {
                var logger = host.Services.GetRequiredService<Serilog.ILogger>();
                if (logger is Serilog.Core.Logger seriLog) 
                {
                    seriLog.Dispose();
                }
            });
            
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
