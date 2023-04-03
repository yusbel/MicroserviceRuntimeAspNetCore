using Sample.Sdk.Data.Constants;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;
using Sample.Sdk.Data.Registration;
using static Sample.Sdk.Data.Registration.RuntimeServiceInfo;
using Sample.Sdk.Data.Exceptions;
using SampleSdkRuntime.Host;
using Serilog;

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
            var runtimeTokenSource = new CancellationTokenSource();
            var runtimeToken = runtimeTokenSource.Token;

            Console.CancelKeyPress += delegate
            {
                runtimeTokenSource.Cancel();                    
            };

            if (args.Count() < 2)
                throw new RuntimeStartException("Service runtime must receive a value setting as an argument with the service instance identifier");
            
            ConfigureEnvironmentVariables.AssignEnvironmentVariables(args);
            ILogger<ServiceRuntime> logger = GetLogger();

            
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ServiceRegistration? serviceReg = null;
            try
            {
                serviceReg = await RunSetupAsync(args, sw, runtimeToken).ConfigureAwait(false);
                if (serviceReg == null)
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
                try
                {
                    await HostService.Run(serviceHostBuilder, serviceReg, args, runtimeToken)
                                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogException(logger.LogCritical);
                }
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
        private static async Task<ServiceRegistration?> 
            RunSetupAsync(string[] args, 
                            Stopwatch sw, 
                            CancellationToken cancellationToken) 
        {
            var serviceInstance = $"{args[0]}-{args[1]}";
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var setupToken = tokenSource.Token;
            var hostRuntime = HostRuntime.Create(args, setupToken);
            var logger = GetLogger();
            (bool isValid, ServiceRegistration? serviceReg, FaultyType? reason) checkRuntimeResult;
            try
            {
                checkRuntimeResult = await CheckRuntimeService<ServiceRegistration>(
                                                    sw,
                                                    TimeSpan.FromMinutes(5),
                                                    TimeSpan.FromMilliseconds(100),
                                                    setupToken);
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
                await hostRuntime.StopAsync().ConfigureAwait(false);
                hostRuntime.Dispose();
                throw;
            }
            
        }

        private static async Task<(bool isValid, T? setupInfo, FaultyType? reason)> 
            CheckRuntimeService<T>(
            Stopwatch sw, 
            TimeSpan duration,
            TimeSpan delay,
            CancellationToken token) where T : RuntimeServiceInfo, new()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ConfigVar.RUNTIME_SETUP_INFO))) 
            {
                T? info;
                try
                {
                    info = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(Convert.FromBase64String(Environment.GetEnvironmentVariable(ConfigVar.RUNTIME_SETUP_INFO)!)));
                }
                catch (Exception)
                {
                    throw;
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
                            token).ConfigureAwait(false);
        }

    }
}