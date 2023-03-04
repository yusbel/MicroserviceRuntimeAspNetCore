using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Sample.Sdk;
using SampleSdkRuntime.Azure.ActiveDirectoryLibs.AppRegistration;
using SampleSdkRuntime.Azure.ActiveDirectoryLibs.ServiceAccount;
using SampleSdkRuntime.Azure.DataOptions;
using SampleSdkRuntime.Azure.Factory;
using SampleSdkRuntime.Azure.Factory.Interfaces;
using SampleSdkRuntime.Azure.KeyVaultLibs;
using SampleSdkRuntime.Azure.KeyVaultLibs.Interfaces;
using SampleSdkRuntime.HostedServices;
using SampleSdkRuntime.HostedServices.Interfaces;
using SampleSdkRuntime.Providers;
using SampleSdkRuntime.Providers.Data;
using SampleSdkRuntime.Providers.Interfaces;
using SampleSdkRuntime.Providers.RuntimeObservers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace SampleSdkRuntime
{
    public static class RuntimeServiceDependencies
    {
        public static IServiceCollection AddRuntimeServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddSampleSdk(config, "Employee:AzureServiceBusInfo:Configuration");
            services.AddRuntimeServiceBusClientAdmin(config);
            services.AddRuntimeServiceSettings(config);
            services.Configure<RuntimeAzureOptions>(config.GetSection("RuntimeAzureOptions"));
            return services;
        }
        private static IServiceCollection AddRuntimeServiceSettings(this IServiceCollection services, IConfiguration configuration) 
        {
            services.AddTransient<IApplicationRegistration, ApplicationRegistration>();
            services.AddTransient<IClientOAuthTokenProviderFactory, ClientOAuthTokenProviderFactory>();
            services.AddTransient<IKeyVaultPolicyProvider, KeyVaultPolicyProvider>();
            services.AddTransient<IGraphServiceClientFactory, GraphServiceClientFactory>();
            services.AddTransient<IArmClientFactory, ArmClientFactory>();
            services.AddTransient<IServicePrincipalProvider, ServicePrincipalProvider>();
            services.AddTransient<IRuntimeVerificationProvider, RuntimeVerificationProvider>();
            services.AddTransient<IRuntimeRepairProvider, RuntimeRepairProvider>();
            services.AddTransient<IAsyncObserver<RuntimeVerificationEvent, VerificationResult>, ServicePrincipalVerificationObserver>();
            services.AddTransient<IAsyncObserver<RuntimeVerificationEvent, VerificationResult>, AppRegVerifictionObserver>(); 
            services.AddTransient<IAsyncObserver<VerificationResult, VerificationRepairResult>, ServicePrincipalRepairObserver>();
            services.AddTransient<IAsyncObserver<VerificationResult, VerificationRepairResult>, AppRegRepairObserver>();
            services.AddTransient<IKeyVaultProvider, KeyVaultProvider>();
            services.AddTransient<IRuntimeVerificationService, RuntimeVerificationService>();
            services.Configure<RuntimeAzureOptions>(configuration.GetSection("Empty"));
            return services;
        }
        private static IServiceCollection AddRuntimeServiceBusClientAdmin(this IServiceCollection services, IConfiguration config)
        {
            services.AddAzureClients(azureClientFactoryBuilder =>
            {
                var serviceBusNamespace = config.GetValue<string>("");
                azureClientFactoryBuilder.AddServiceBusAdministrationClientWithNamespace(serviceBusNamespace);
                //Read azure key vault uri
                var keyVaultStringUri = string.IsNullOrEmpty(config.GetValue<string>("ServiceSdk:Security:AzureKeyVaultOptions:VaultUri")) ? "https://learningkeyvaultyusbel.vault.azure.net/"
                                                                    : config.GetValue<string>("ServiceSdk:Security:AzureKeyVaultOptions:VaultUri");
                var keyVaultUri = new Uri(keyVaultStringUri);
                azureClientFactoryBuilder.AddCertificateClient(keyVaultUri);
                azureClientFactoryBuilder.AddSecretClient(keyVaultUri);//.WithCredential(new DefaultAzureCredential());
                //azureClientFactoryBuilder.UseCredential(new DefaultAzureCredential());
                azureClientFactoryBuilder.UseCredential((services) =>
                {
                    var clientCredentialFactory = services.GetRequiredService<IClientOAuthTokenProviderFactory>();
                    clientCredentialFactory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecretCredential);
                    //var creds = clientCredentialFactory.GetDefaultCredential();
                    //var credentialOptions = new DefaultAzureCredentialOptions()
                    //{
                    //    ManagedIdentityClientId = creds.clientId,
                    //    TenantId = creds.tenantId, 
                    //    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                    //};
                    return clientSecretCredential;// new DefaultAzureCredential(credentialOptions);
                });

            });
            return services;
        }
    }
}
