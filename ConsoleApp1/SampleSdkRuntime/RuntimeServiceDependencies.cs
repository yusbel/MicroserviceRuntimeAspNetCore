using Microsoft.Azure.Cosmos;
using Sample.Sdk.Core;
using Sample.Sdk.Interface;
using Sample.Sdk.Interface.Azure.Factory;
using Sample.Sdk.Interface.Registration;
using SampleSdkRuntime.HostedServices;
using SampleSdkRuntime.HostedServices.Interfaces;
using SampleSdkRuntime.Providers;
using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;
using SampleSdkRuntime.Providers.RuntimeObservers;

namespace SampleSdkRuntime
{
    public static class RuntimeServiceDependencies
    {
        public static IServiceCollection AddRuntimeServices(this IServiceCollection services, IConfiguration config, IServiceContext serviceContext)
        {
            services.AddRuntimeServiceSettings(config);
            services.AddServicesCore(config);
            return services;
        }
        private static IServiceCollection AddRuntimeServiceSettings(this IServiceCollection services, IConfiguration configuration) 
        {
            services.AddTransient<IRuntimeVerificationProvider, RuntimeVerificationProvider>();
            services.AddTransient<IRuntimeRepairProvider, RuntimeRepairProvider>();
            services.AddTransient<IAsyncObserver<RuntimeVerificationEvent, VerificationResult>, ServicePrincipalVerificationObserver>();
            services.AddTransient<IAsyncObserver<RuntimeVerificationEvent, VerificationResult>, AppRegVerifictionObserver>(); 
            services.AddTransient<IAsyncObserver<VerificationResult, VerificationRepairResult>, ServicePrincipalRepairObserver>();
            services.AddTransient<IAsyncObserver<VerificationResult, VerificationRepairResult>, AppRegRepairObserver>();
            services.AddTransient<IRuntimeVerificationService, RuntimeVerificationService>();
            services.AddTransient<IServiceRegistrationProvider, ServiceRegistrationProvider>();

            services.AddSingleton(sp => 
            {
                var tokenClientFactory = sp.GetRequiredService<IClientOAuthTokenProviderFactory>();
                var clientSecretCredential = tokenClientFactory.GetClientSecretCredential();
                var cosmosDbClient = new CosmosClient("https://microservice-service-runtime.documents.azure.com:443", clientSecretCredential);
                
                return cosmosDbClient;
            });

            return services;
        }

        
        
    }
}
