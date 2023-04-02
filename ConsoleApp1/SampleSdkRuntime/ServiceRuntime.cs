using Azure.Data.AppConfiguration;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sample.Sdk.Core.Azure.Factory;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Enums;
using Sample.Sdk.Data.Options;
using SampleSdkRuntime.Data;
using SampleSdkRuntime.HostedServices;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;
using Sample.Sdk.Interface;
using Sample.Sdk.Data.Registration;
using static Sample.Sdk.Data.Registration.RuntimeServiceInfo;
using Sample.Sdk.Data.Exceptions;
using Sample.Sdk.Core;

namespace SampleSdkRuntime
{
    public class ServiceRuntime
    {
        /// <summary>
        /// ServiceRuntime-ServiceBusClient: 8d69558c-fc01-407c-becc-e561358afafb
        /// TenantId: c8656f45-daf5-42c1-9b29-ac27d3e63bf3
        /// Secret: ySk8Q~ZNd12SZ6bk-UEBzpwwqi1Ks8Fohy~t4clt
        /// 
        /// ServiceRuntime-AzureKeyVaultClient: 96306b29-66b5-4420-bef1-af12fa9677d0
        /// TenantId: c8656f45-daf5-42c1-9b29-ac27d3e63bf3
        /// Secret: My38Q~kWXP1evnP1iOIxETYUMb6FZgCz~gGZPbyG
        /// 
        /// ServiceRuntime-AdPrincipleAccount: 0f691c02-1c41-4783-b54c-22d921db4e16
        /// TenantID: c8656f45-daf5-42c1-9b29-ac27d3e63bf3
        /// Secret: HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x
        /// 
        /// </summary>
        /// <param name="args">Pass client id with prmission to query secrets on azure key vault</param>
        /// <param name="host"></param>
        /// <returns></returns>
        public static async Task RunAsync(string[] args, IHostBuilder serviceHostBuilder = null)
        {
            if (args.Count() < 2)
                throw new RuntimeStartException("Service runtime must receive a value setting as an argument with the service instance identifier");
            AssignEnvironmentVariables(args);

            ILogger<ServiceRuntime> logger = GetLogger();

            var runtimeTokenSource = new CancellationTokenSource();
            var runtimeToken = runtimeTokenSource.Token;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ServiceRegistration? serviceReg = null;
            try
            {
                serviceReg = await RunSetupAsync(args, sw, runtimeToken).ConfigureAwait(false);
                if (serviceReg != null)
                {
                    sw.Stop();
                    logger.LogCritical("Setup fail, service won't start");
                    return;
                }
                sw.Stop();
            }
            catch (Exception e)
            {
                e.LogException(logger.LogCritical);
                sw.Stop();
                return;
            }
            //Running the service host after runtime setup the service
            if (serviceHostBuilder != null && serviceReg != null)
            {
                var serviceHostVariables = new Dictionary<string, string>
                {
                    { ConfigVarConst.AZURE_TENANT_ID, Environment.GetEnvironmentVariable(ConfigVarConst.AZURE_TENANT_ID)! },
                    { ConfigVarConst.AZURE_CLIENT_ID, serviceReg.Credentials.First().ClientId },
                    { ConfigVarConst.AZURE_CLIENT_SECRET, serviceReg.Credentials.First().ServiceSecretText },
                    { ConfigVarConst.SERVICE_INSTANCE_NAME_ID, serviceReg.ServiceInstanceId },
                    { ConfigVarConst.IS_RUNTIME, "false" }
                };
                serviceHostBuilder.ConfigureAppConfiguration((host, builder) =>
                {
                    builder.AddInMemoryCollection(serviceHostVariables)
                    .AddAzureAppConfiguration(appConfig =>
                    {
                        appConfig.Connect(Environment.GetEnvironmentVariable(ConfigVarConst.APP_CONFIG_CONN_STR));
                        appConfig.Select(KeyFilter.Any, LabelFilter.Null);
                        appConfig.Select(KeyFilter.Any, Environment.GetEnvironmentVariable(ConfigVarConst.ENVIRONMENT_VAR));
                        appConfig.ConfigureKeyVault(configureKeyVault =>
                        {
                            var serviceVaultUri = ServiceConfiguration.Create(host.Configuration)
                                                    .GetKeyVaultUri(Enums.HostTypeOptions.ServiceInstance);
                            var tokenClientFactory = new ClientOAuthTokenProviderFactory(host.Configuration);
                            tokenClientFactory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var tokenClient);
                            var secretClient = new SecretClient(new Uri(serviceVaultUri), tokenClient);
                            configureKeyVault.Register(secretClient);
                            configureKeyVault.SetSecretResolver(async keyVaultUri =>
                            {
                                var serviceContext = new ServiceContext(ServiceRegistration.DefaultInstance(Environment.GetEnvironmentVariable(ConfigVarConst.SERVICE_INSTANCE_NAME_ID)!));
                                var secretResult = await secretClient.GetSecretAsync($"{serviceContext.GetServiceInstanceName()}{serviceContext.GetServiceBlobConnStrKey()}");
                                return secretResult.Value.Value;
                            });
                        });
                    });
                });
                serviceHostBuilder.ConfigureServices((host, services) =>
                {
                    services.TryAddSingleton<IServiceContext>(serviceProvider =>
                    {
                        var serviceContext = new ServiceContext(serviceReg);
                        return serviceContext;
                    });
                    services.AddRuntimeServices(host.Configuration, new ServiceContext(serviceReg));
                });
                try
                {
                    var app = serviceHostBuilder.Build();
                    await app.RunAsync(runtimeToken);
                }
                catch (Exception e)
                {
                    e.LogException(logger.LogCritical);
                }

            }
        }

        private static void AssignEnvironmentVariables(string[] args)
        {
            Environment.SetEnvironmentVariable(ConfigVarConst.AZURE_TENANT_ID, "c8656f45-daf5-42c1-9b29-ac27d3e63bf3");
            Environment.SetEnvironmentVariable(ConfigVarConst.ENVIRONMENT_VAR, "Development");
            Environment.SetEnvironmentVariable(ConfigVarConst.AZURE_CLIENT_ID, "0f691c02-1c41-4783-b54c-22d921db4e16");
            Environment.SetEnvironmentVariable(ConfigVarConst.AZURE_CLIENT_SECRET, "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x");
            Environment.SetEnvironmentVariable(ConfigVarConst.APP_CONFIG_CONN_STR, "Endpoint=https://learningappconfig.azconfig.io;Id=pIlK-ll-s0:SMHTAi4UoZxaK1C0ADZg;Secret=5cx53U0WM7bLwCcoJ2nM0oit+B1MK7UUsbWA9p6z3KY=");
            Environment.SetEnvironmentVariable(ConfigVarConst.SERVICE_DATA_BLOB_CONTAINER_NAME, "servicedata");
            Environment.SetEnvironmentVariable(ConfigVarConst.SERVICE_BLOB_CONN_STR_APP_CONFIG_KEY, "MessageSignatureBlobConnSt");
            Environment.SetEnvironmentVariable(ConfigVarConst.SERVICE_INSTANCE_NAME_ID, $"{args[0]}-{args[1]}");
            Environment.SetEnvironmentVariable(ConfigVarConst.BLOB_CERTIFICATE_PATH_APP_CONFIG_KEY, "ServiceRuntime:BlobCertificatePath");
            Environment.SetEnvironmentVariable(ConfigVarConst.SERVICE_RUNTIME_CERTIFICATE_NAME_APP_CONFIG_KEY, "ServiceRuntime:SignatureCertificateName");
            Environment.SetEnvironmentVariable(ConfigVarConst.SERVICE_RUNTIME_BLOB_CONN_STR_KEY, "ServiceRuntime:BlobPublicKeyConnStr");
            Environment.SetEnvironmentVariable(ConfigVarConst.RUNTIME_BLOB_PUBLICKEY_CONTAINER_NAME, "ServiceRuntime:BlobPublicKeys:ContainerName");
            Environment.SetEnvironmentVariable(ConfigVarConst.DB_CONN_STR, "DbConnectionString");
            Environment.SetEnvironmentVariable(ConfigVarConst.CUSTOM_PROTOCOL, "ServiceSdk:Security:CustomProtocol");
            Environment.SetEnvironmentVariable(ConfigVarConst.AZURE_MESSAGE_SETTINGS, "Service:AzureMessageSettings:Configuration");
        }

        private static ILogger<ServiceRuntime> GetLogger()
        {
            return LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddFilter(loglevel =>
                {
                    loglevel = LogLevel.Information;
                    return true;
                });
                builder.AddConsole();
            }).CreateLogger<ServiceRuntime>();
        }

        /// <summary>
        /// Create a setup hosted service to configure and valid the service dependecies.
        /// This task would be called on an schedule to keep the service dependecies configure as per configuration.
        /// TODO: Add test cases.
        /// </summary>
        /// <param name="args">arguments passed from command line</param>
        /// <param name="sw">stop watch to timeout in case of configuration error</param>
        /// <param name="cancellationToken">runtime token to stop the setup hosted service</param>
        /// <returns>valid when the service computed as expected. faulty if the service dependencies were not configured prperly</returns>
        /// <exception cref="RuntimeStartException">Throw the exception if the runtime is unable to setup the service</exception>
        private static async Task<ServiceRegistration?> 
            RunSetupAsync(string[] args, 
                            Stopwatch sw, 
                            CancellationToken cancellationToken) 
        {
            var serviceInstance = $"{args[0]}-{args[1]}";
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var setupToken = tokenSource.Token;
            var keyValuePair = new Dictionary<string, string>() 
            {
                { ConfigVarConst.RUNTIME_AZURE_TENANT_ID, "c8656f45-daf5-42c1-9b29-ac27d3e63bf3" },
                { ConfigVarConst.RUNTIME_AZURE_CLIENT_ID, "0f691c02-1c41-4783-b54c-22d921db4e16" },
                { ConfigVarConst.RUNTIME_AZURE_CLIENT_SECRET, "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x" },
                { ConfigVarConst.IS_RUNTIME, "true" },
                { ConfigVarConst.SERVICE_INSTANCE_NAME_ID, serviceInstance }
            };

            IHost hostRuntimeSetup = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                
                .ConfigureAppConfiguration((host,configBuilder) => 
                {
                    configBuilder.AddInMemoryCollection(keyValuePair);
                    configBuilder.AddAzureAppConfiguration(appConfig =>
                    {
                        appConfig.Connect(Environment.GetEnvironmentVariable(ConfigVarConst.APP_CONFIG_CONN_STR));
                        appConfig.Select(KeyFilter.Any, LabelFilter.Null);
                        appConfig.Select(KeyFilter.Any, Environment.GetEnvironmentVariable(ConfigVarConst.ENVIRONMENT_VAR));
                        appConfig.ConfigureKeyVault(configureKeyVault =>
                        {
                            var appConfigClient = new ConfigurationClient(Environment.GetEnvironmentVariable(ConfigVarConst.APP_CONFIG_CONN_STR));
                            var keyVaultConfigAppKey = $"{args[0]}:{AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION_APP_CONFIG}";
                            var serviceVaultUri = appConfigClient.GetConfigurationSetting(
                                keyVaultConfigAppKey, 
                                Environment.GetEnvironmentVariable(ConfigVarConst.ENVIRONMENT_VAR));

                            var keyVaultOptions = JsonSerializer.Deserialize<AzureKeyVaultOptions>(serviceVaultUri.Value.Value);

                            var tokenClientFactory = new ClientOAuthTokenProviderFactory(host.Configuration);
                            var clientSecretCredential = tokenClientFactory.GetClientSecretCredential();
                            var secretClient = new SecretClient(new Uri(keyVaultOptions!.VaultUri), clientSecretCredential);

                            configureKeyVault.SetSecretResolver(async keyVaultUri => 
                            {
                                var secretName = keyVaultUri.AbsolutePath.Substring("/secrets/".Length, keyVaultUri.AbsolutePath.Length - "/secrets/".Length);
                                var keyVaultStr = keyVaultUri.ToString().Substring(0, keyVaultUri.ToString().Length - keyVaultUri.AbsolutePath.Length);
                                var secretClient = new SecretClient(new Uri(keyVaultStr), clientSecretCredential);
                                var secret = await secretClient.GetSecretAsync(secretName).ConfigureAwait(false);
                                return secret.Value.Value;
                            });
                        });
                    });
                })
                .ConfigureServices((host, services) =>
                {
                    //ServiceConfiguration.Create(host.Configuration).AddRuntimeAzureKeyVaultOptions(services);
                    
                    services.AddRuntimeServices(host.Configuration, new ServiceContext(ServiceRegistration.DefaultInstance(serviceInstance)));
                    services.AddTransient<IServiceContext>(sp => 
                    {
                        return new ServiceContext(ServiceRegistration.DefaultInstance(serviceInstance));
                    });
                    services.AddHostedService<RuntimeSetupHostedAppService>();
                }).Build();
            
            var configuration = hostRuntimeSetup.Services.GetRequiredService<IConfiguration>();
            var logger = hostRuntimeSetup.Services.GetRequiredService<ILogger<ServiceRuntime>>();
            try
            {
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await hostRuntimeSetup.RunAsync(setupToken).ConfigureAwait(false);
                    }
                    catch (Exception e) 
                    {
                        e.LogException(logger.LogCritical);
                    }
                }, setupToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogException(logger.LogCritical);
                tokenSource.Cancel();
                tokenSource.Dispose();
                throw;
            }

            (bool isValid, ServiceRegistration? serviceReg, FaultyType? reason) checkRuntimeResult;
            try
            {
                checkRuntimeResult = await CheckRuntimeService<ServiceRegistration>(
                                                    sw,
                                                    TimeSpan.FromMinutes(5),
                                                    TimeSpan.FromMilliseconds(100),
                                                    setupToken,
                                                    configuration,
                                                    logger);
                if (!checkRuntimeResult.serviceReg!.WasSuccessful 
                    && checkRuntimeResult.reason == FaultyType.TimeOutReached)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    throw new RuntimeStartException("Time out was reached");
                }
                return checkRuntimeResult.serviceReg;
            }
            catch (Exception e)
            {
                e.LogException(logger.LogCritical);
                tokenSource.Cancel();
                await hostRuntimeSetup.StopAsync().ConfigureAwait(false);
                hostRuntimeSetup.Dispose();
                throw;
            }
            
        }

        private static async Task<(bool isValid, T? setupInfo, FaultyType? reason)> 
            CheckRuntimeService<T>(
            Stopwatch sw, 
            TimeSpan duration,
            TimeSpan delay,
            CancellationToken token, 
            IConfiguration configuration,
            ILogger logger) where T : RuntimeServiceInfo, new()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ConfigVarConst.RUNTIME_SETUP_INFO))) 
            {
                T? info;
                try
                {
                    info = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(Convert.FromBase64String(Environment.GetEnvironmentVariable(ConfigVarConst.RUNTIME_SETUP_INFO)!)));
                }
                catch (Exception e)
                {
                    e.LogException(logger.LogCritical);
                    return (false, default, FaultyType.InfoDataTypeMissMatch);
                }
                if(info != null) 
                {
                    return (info.WasSuccessful, info, default);
                }
                return (true, info, FaultyType.InfoDataTypeMissMatch);
            }
            if (duration > TimeSpan.Zero && sw.Elapsed >= duration) 
            {
                return (false, default, FaultyType.TimeOutReached);
            }
            if(token.IsCancellationRequested) 
            {
                token.ThrowIfCancellationRequested();
            }
            await Task.Delay(delay).ConfigureAwait(false);

            return await CheckRuntimeService<T>(
                            sw, 
                            duration,
                            delay,
                            token, 
                            configuration, 
                            logger).ConfigureAwait(false);
        }

    }
}