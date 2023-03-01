using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Exceptions;
using SampleSdkRuntime.Data;
using SampleSdkRuntime.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using static SampleSdkRuntime.Data.IRuntimeServiceInfo;

namespace SampleSdkRuntime
{
    public class ServiceRuntime
    {
        private static string SetupInfo = "SetupInfo";

        ///

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
        public static async Task RunAsync(string[] args, IHost host = null)
        {
            var logger = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddFilter(loglevel =>
                {
                    loglevel = LogLevel.Information;
                    return true;
                });
                builder.AddConsole();
            }).CreateLogger<ServiceRuntime>();

            var runtimeTokenSource = new CancellationTokenSource();
            var runtimeToken = runtimeTokenSource.Token;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            (bool isValid, bool isFaulty, FaultyType reason) setupResult;
            try
            {
                setupResult = await RunSetupAsync(args, sw, runtimeToken);
                if (!setupResult.isValid) 
                {
                    sw.Stop();
                    logger.LogCritical("Setup fail, service wont start");
                    return;
                }
            }
            catch (Exception e) 
            {
                e.LogCriticalException(logger, "An error ocurred when settting up the service");
                sw.Stop();
                return;
            }
            if (host != null) 
            {
                //Configure the host service and run it with the runtime cancellation token
                var hostAppLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                await host.RunAsync(runtimeToken);
            }
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
        private static async Task<(bool isValid, bool isFaulty, FaultyType reason)> 
            RunSetupAsync(string[] args, Stopwatch sw, CancellationToken cancellationToken) 
        {
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var setupToken = tokenSource.Token;

            IHost hostRuntimeSetup = Host.CreateDefaultBuilder(args)
                .ConfigureServices((host, services) =>
                {
                    services.AddRuntimeServices(host.Configuration);
                    services.AddHostedService<RuntimeSetupHostedAppService>();
                }).Build();
            var configuration = hostRuntimeSetup.Services.GetRequiredService<IConfiguration>();
            var logger = hostRuntimeSetup.Services.GetRequiredService<ILogger>();
            Task? runtimeSetupTask;
            try
            {
                runtimeSetupTask = Task.Run(async () => await hostRuntimeSetup.RunAsync(setupToken), setupToken);
                await Task.Delay(100);
            }
            catch (Exception e)
            {
                e.LogCriticalException(logger, "Runtime task fail");
                tokenSource.Cancel();
                tokenSource.Dispose();
                throw;
            }
            //clean setup info from environment
            Environment.SetEnvironmentVariable(SetupInfo, string.Empty);

            (bool isValid, bool isFaulty, FaultyType reason) checkRuntimeResult;
            try
            {
                checkRuntimeResult = await CheckRuntimeService<IRuntimeServiceInfo>(
                                                    sw,
                                                    TimeSpan.FromMinutes(5),
                                                    TimeSpan.FromMilliseconds(100),
                                                    setupToken,
                                                    configuration,
                                                    logger,
                                                    SetupInfo);
                if (checkRuntimeResult.isFaulty
                    && checkRuntimeResult.reason == FaultyType.TimeOutReached)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    throw new RuntimeStartException("Time out was reached");
                }
                if (checkRuntimeResult.isFaulty
                    && checkRuntimeResult.reason == FaultyType.InfoDataTypeMissMatch)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    throw new RuntimeStartException("Runtime info environemnt can't be deserialized to runtime info");
                }
                return checkRuntimeResult;
            }
            catch (Exception e)
            {
                e.LogCriticalException(logger, "check runtime setup fail");
                throw;
            }
            finally { tokenSource.Dispose(); }
            
        }

        private static async Task<(bool isValid, bool isFaulty, FaultyType reason)> 
            CheckRuntimeService<T>(
            Stopwatch sw, 
            TimeSpan duration,
            TimeSpan delay,
            CancellationToken token, 
            IConfiguration configuration,
            ILogger logger,
            string configurationInfoIdentifier) where T : IRuntimeServiceInfo
        {
            if (!string.IsNullOrEmpty(configuration.GetValue<string>(configurationInfoIdentifier))) 
            {
                IRuntimeServiceInfo? info;
                try
                {
                    info = JsonSerializer.Deserialize<T>(configuration.GetValue<string>(configurationInfoIdentifier));
                }
                catch (Exception e)
                {
                    e.LogCriticalException(logger, "Deserializing fail");
                    return (true, false, FaultyType.InfoDataTypeMissMatch);
                }
                if(info != null) 
                {
                    return (info.IsValid, info.IsFaulty, default(FaultyType));
                }
                return (true, false, FaultyType.InfoDataTypeMissMatch);
            }
            if (duration > TimeSpan.Zero && sw.Elapsed >= duration) 
            {
                return (false, true, FaultyType.TimeOutReached);
            }
            if(token.IsCancellationRequested) 
            {
                token.ThrowIfCancellationRequested();
            }
            await Task.Delay(delay);
            return await CheckRuntimeService<IRuntimeServiceInfo>(
                            sw, 
                            duration,
                            delay,
                            token, 
                            configuration, 
                            logger, 
                            configurationInfoIdentifier);
        }

    }
}