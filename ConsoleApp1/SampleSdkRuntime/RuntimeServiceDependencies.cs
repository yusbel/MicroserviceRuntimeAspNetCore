using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Sample.Sdk;
using SampleSdkRuntime.Azure.DataOptions;
using SampleSdkRuntime.Azure.Factory;
using SampleSdkRuntime.Azure.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime
{
    public static class RuntimeServiceDependencies
    {
        public static IServiceCollection AddRuntimeServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddSampleSdkServiceBusClient(config);
            services.AddRuntimeServiceBusClientAdmin(config);
            services.AddTransient<IClientTokenCredentialFactory, ClientTokenCredentialFactory>();
            services.AddTransient<IKeyVaultPolicyProvider, KeyVaultPolicyProvider>();
            services.Configure<RuntimeAzureOptions>(config.GetSection("RuntimeAzureOptions"));
            return services;
        }

        private static IServiceCollection AddRuntimeServiceBusClientAdmin(this IServiceCollection services, IConfiguration config)
        {
            services.AddAzureClients(azureClientFactoryBuilder =>
            {
                var serviceBusNamespace = config.GetValue<string>("");
                azureClientFactoryBuilder.AddServiceBusAdministrationClientWithNamespace(serviceBusNamespace);
                //Read azure key vault uri
                var keyVaultUri = new Uri(config.GetValue<string>("ServiceSdk:Security:AzureKeyVaultOptions:VaultUri"));
                azureClientFactoryBuilder.AddSecretClient(keyVaultUri);
                //azureClientFactoryBuilder.UseCredential(new EnvironmentCredential(new TokenCredentialOptions() {  }));
                
            });
            return services;
        }
    }
}
