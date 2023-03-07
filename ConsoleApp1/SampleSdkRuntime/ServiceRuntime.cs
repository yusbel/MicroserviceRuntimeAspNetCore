﻿using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Sdk;
using Sample.Sdk.Core.Exceptions;
using SampleSdkRuntime.Data;
using SampleSdkRuntime.Exceptions;
using SampleSdkRuntime.HostedServices;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using static SampleSdkRuntime.Data.IRuntimeServiceInfo;

namespace SampleSdkRuntime
{
    public class ServiceRuntime
    {
        public const string IS_RUNTIME = "IsRuntime";
        public const string AZURE_TENANT_ID = "AZURE_TENANT_ID";
        public const string AZURE_CLIENT_ID = "AZURE_CLIENT_ID";
        public const string AZURE_CLIENT_SECRET = "AZURE_CLIENT_SECRET";

        public const string RUNTIME_AZURE_TENANT_ID = "RUNTIME:AZURE_TENANT_ID";
        public const string RUNTIME_AZURE_CLIENT_ID = "RUNTIME:AZURE_CLIENT_ID";
        public const string RUNTIME_AZURE_CLIENT_SECRET = "RUNTIME:AZURE_CLIENT_SECRET";

        public const string SERVICE_INSTANCE_ID = "SERVICE_INSTANCE_ID";
        public const string RUNTIME_SETUP_INFO = "SetupInfo";

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
            //Environment.SetEnvironmentVariable(AZURE_TENANT_ID, "c8656f45-daf5-42c1-9b29-ac27d3e63bf3");
            //Environment.SetEnvironmentVariable(AZURE_CLIENT_ID, "0f691c02-1c41-4783-b54c-22d921db4e16");
            //Environment.SetEnvironmentVariable(AZURE_CLIENT_SECRET, "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x");

            //Environment.SetEnvironmentVariable(RUNTIME_AZURE_TENANT_ID, "c8656f45-daf5-42c1-9b29-ac27d3e63bf3");
            //Environment.SetEnvironmentVariable(RUNTIME_AZURE_CLIENT_ID, "0f691c02-1c41-4783-b54c-22d921db4e16");
            //Environment.SetEnvironmentVariable(RUNTIME_AZURE_CLIENT_SECRET, "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x");

            if (args.Length == 0)
                throw new RuntimeStartException("Service runtime must receive a value setting as an argument with the service instance identifier");
            ILogger<ServiceRuntime> logger = GetLogger();

            var runtimeTokenSource = new CancellationTokenSource();
            var runtimeToken = runtimeTokenSource.Token;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            (bool isValid, SetupInfo? setupInfo, FaultyType? reason, IHost host) setupResult;
            try
            {
                setupResult = await RunSetupAsync(args, sw, runtimeToken).ConfigureAwait(false);
                if (!setupResult.isValid)
                {
                    sw.Stop();
                    logger.LogCritical("Setup fail, service won't start");
                    return;
                }
                sw.Stop();
            }
            catch (Exception e)
            {
                e.LogCriticalException(logger, "An error ocurred when settting up the service");
                sw.Stop();
                return;
            }

            //To work on tomorrow
            if (serviceHostBuilder != null && setupResult.isValid && setupResult.setupInfo != null)
            {
                var serviceHostVariables = new Dictionary<string, string>
                {
                    { AZURE_TENANT_ID, setupResult.setupInfo.Value.ServiceAccountInfo.TenantId },
                    { AZURE_CLIENT_ID, setupResult.setupInfo.Value.ServiceAccountInfo.ApplicationClientId },
                    { AZURE_CLIENT_SECRET, setupResult.setupInfo.Value.ServiceAccountInfo.ClientSecret },
                    { IS_RUNTIME, "false" }
                };
                serviceHostBuilder.ConfigureAppConfiguration(builder => 
                {
                    builder.AddInMemoryCollection(serviceHostVariables);
                });
                serviceHostBuilder.ConfigureServices((host, services) => 
                {
                    services.AddRuntimeServices(host.Configuration);
                });
                var app = serviceHostBuilder.Build();
                var hostAppLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
                await app.RunAsync(runtimeToken);
            }
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
        private static async Task<(bool isValid, SetupInfo? setupInfo, FaultyType? reason, IHost? host)> 
            RunSetupAsync(string[] args, 
                            Stopwatch sw, 
                            CancellationToken cancellationToken) 
        {
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var setupToken = tokenSource.Token;
            var keyValuePair = new Dictionary<string, string>() 
            {
                { RUNTIME_AZURE_TENANT_ID, "c8656f45-daf5-42c1-9b29-ac27d3e63bf3" },
                { RUNTIME_AZURE_CLIENT_ID, "0f691c02-1c41-4783-b54c-22d921db4e16" },
                { RUNTIME_AZURE_CLIENT_SECRET, "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x" },
                { IS_RUNTIME, "true" },
                { SERVICE_INSTANCE_ID, args.First() }
            };
            IHost hostRuntimeSetup = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configBuilder => 
                {
                    configBuilder.AddInMemoryCollection(keyValuePair);
                })
                .ConfigureServices((host, services) =>
                {
                    services.AddSampleSdkAzureKeyVaultCertificateAndSecretClient(host.Configuration);
                    services.AddRuntimeServices(host.Configuration);
                    services.AddHostedService<RuntimeSetupHostedAppService>();
                }).Build();
            var configuration = hostRuntimeSetup.Services.GetRequiredService<IConfiguration>();
            var logger = hostRuntimeSetup.Services.GetRequiredService<ILogger<ServiceRuntime>>();
            try
            {
                _ = Task.Run(async () => await hostRuntimeSetup.RunAsync(setupToken).ConfigureAwait(false), setupToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogCriticalException(logger, "Runtime task fail");
                tokenSource.Cancel();
                tokenSource.Dispose();
                throw;
            }

            (bool isValid, SetupInfo? setupInfo, FaultyType? reason) checkRuntimeResult;
            try
            {
                checkRuntimeResult = await CheckRuntimeService<SetupInfo>(
                                                    sw,
                                                    TimeSpan.FromMinutes(5),
                                                    TimeSpan.FromMilliseconds(100),
                                                    setupToken,
                                                    configuration,
                                                    logger);
                if (checkRuntimeResult.setupInfo.HasValue && checkRuntimeResult.setupInfo.Value.IsFaulty
                    && checkRuntimeResult.reason == FaultyType.TimeOutReached)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    throw new RuntimeStartException("Time out was reached");
                }
                if (checkRuntimeResult.setupInfo.HasValue && checkRuntimeResult.setupInfo.Value.IsFaulty
                    && checkRuntimeResult.reason == FaultyType.InfoDataTypeMissMatch)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    throw new RuntimeStartException("Runtime info environemnt can't be deserialized to runtime info");
                }
                if (!checkRuntimeResult.isValid)
                {
                    tokenSource.Cancel();
                    await hostRuntimeSetup.StopAsync().ConfigureAwait(false);
                    hostRuntimeSetup.Dispose();
                }
                return (checkRuntimeResult.isValid, 
                        checkRuntimeResult.setupInfo, 
                        checkRuntimeResult.reason, 
                        !checkRuntimeResult.isValid ? default : hostRuntimeSetup);
            }
            catch (Exception e)
            {
                e.LogCriticalException(logger, "check runtime setup fail");
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
            ILogger logger) where T : IRuntimeServiceInfo, new()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(RUNTIME_SETUP_INFO))) 
            {
                T? info;
                try
                {
                    info = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(Convert.FromBase64String(Environment.GetEnvironmentVariable(RUNTIME_SETUP_INFO))));
                }
                catch (Exception e)
                {
                    e.LogCriticalException(logger, "Deserializing fail");
                    return (false, default, FaultyType.InfoDataTypeMissMatch);
                }
                if(info != null) 
                {
                    return (info.IsValid, info, default);
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
            await Task.Delay(delay);

            return await CheckRuntimeService<T>(
                            sw, 
                            duration,
                            delay,
                            token, 
                            configuration, 
                            logger);
        }

    }
}