using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk.Core.Azure.Factory;
using Sample.Sdk.Core.DatabaseContext;
using Sample.Sdk.Core.Msg;
using Sample.Sdk.Core.Security.Asymetric;
using Sample.Sdk.Core.Security.Certificate;
using Sample.Sdk.Core.Security.Signature;
using Sample.Sdk.Core.Security.Symetric;
using Sample.Sdk.Core.Security;
using Sample.Sdk.Data.Options;
using Sample.Sdk.Interface.Azure.Factory;
using Sample.Sdk.Interface.Msg;
using Sample.Sdk.Interface.Security.Asymetric;
using Sample.Sdk.Interface.Security.Certificate;
using Sample.Sdk.Interface.Security;
using Sample.Sdk.Interface.Security.Keys;
using Sample.Sdk.Interface.Security.Signature;
using Sample.Sdk.Interface.Security.Symetric;
using static Sample.Sdk.Data.Enums.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Sample.Sdk.Core
{
    public static class ServiceCollectionRegistration
    {
        public static IServiceCollection AddServicesCore(this IServiceCollection services, IConfiguration config) 
        {
            var configOptions = ServiceConfiguration.Create(config);
            configOptions.AddDatabaseSettingsOptions(services);
            configOptions.AddCustomProtocolOptions(services);
            services.Configure<MessageSettingsConfigurationOptions>(config.GetSection(MessageSettingsConfigurationOptions.SectionIdentifier));

            services.AddTransient<IArmClientFactory, ArmClientFactory>();
            services.AddTransient<IClientOAuthTokenProviderFactory, ClientOAuthTokenProviderFactory>();
            services.AddTransient<IGraphServiceClientFactory, GraphServiceClientFactory>();
            services.AddDbContext<ServiceDbContext>();
            services.AddSingleton<HttpClient>();
            services.AddSingleton<IPublicKeyProvider, IPublicKeyProvider>();
            services.AddTransient<ISendExternalMessage, MessageSenderService>();
            services.AddTransient<IMessageInTransitService, MessageInTransitService>();
            services.AddTransient<IClientOAuthTokenProviderFactory, ClientOAuthTokenProviderFactory>();

            services.AddAzureKeyVaultClients(config);
            services.AddCryptographic();
            services.AddServiceBus(config);
            services.AddInMemoryServices(config);

            return services;
        }

        public static IServiceCollection AddInMemoryServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddHostedService<MessageSenderRealtimeHostedService>();
            services.AddHostedService<MessageReceiverRealtimeHostedService>();
            services.AddTransient<IMessageComputation, ComputeReceivedMessage>();
            services.AddTransient<IMessageRealtimeService, MessageReceiverService>();
            services.AddTransient<IMessageRealtimeService, MessageSenderService>();
            services.AddTransient<IMessageSender, ServiceBusMessageSender>();
            services.AddTransient<IMessageReceiver, ServiceBusMessageReceiver>();
            services.Configure<MemoryCacheOptions>(config);
            services.AddTransient<IMemoryCache, MemoryCache>();
            return services;
        }

        public static IServiceCollection AddServiceBus(this IServiceCollection services, IConfiguration configuration)
        {
            ServiceConfiguration.Create(configuration).AddAzureServiceBusOptions(services);
            services.AddAzureClients(azureClientFactory =>
            {
                var connStr = ServiceConfiguration.Create(configuration).GetServiceBusConnStr();
                if (connStr.receiverConnStr != connStr.senderConnStr)
                    throw new InvalidOperationException("Multiple service bus are not supported");
                azureClientFactory.AddServiceBusClient(connStr.senderConnStr);
            });
            return services;
        }

        public static IServiceCollection AddCryptographic(this IServiceCollection services)
        {
            services.AddTransient<ISignatureCryptoProvider, SignatureCryptoProvider>();
            services.AddTransient<IMessageCryptoService, MessageCryptoService>();
            services.AddTransient<ISecurityEndpointValidator, SecurityEndpointValidator>();
            services.AddTransient<ISymetricCryptoProvider, AesSymetricCryptoProvider>();
            services.AddTransient<IAesKeyRandom, AesSymetricCryptoProvider>();
            services.AddTransient<IAsymetricCryptoProvider, X509CertificateServiceProviderAsymetricAlgorithm>();
            services.AddTransient<ICertificateProvider, AzureKeyVaultCertificateProvider>();

            return services;
        }

        internal static IServiceCollection AddAzureKeyVaultClients(this IServiceCollection services, IConfiguration config)
        {
            var runtimeKeyVaultUri = ServiceConfiguration.Create(config).GetKeyVaultUri(HostTypeOptions.Runtime);
            var serviceKeyVaultUri = ServiceConfiguration.Create(config).GetKeyVaultUri(HostTypeOptions.ServiceInstance);
            services.AddAzureClients(factory =>
            {
                var runtime = HostTypeOptions.Runtime.ToString();
                factory.AddKeyClient(new Uri(runtimeKeyVaultUri)).WithName(HostTypeOptions.Runtime.ToString());
                factory.AddCertificateClient(new Uri(runtimeKeyVaultUri)).WithName(HostTypeOptions.Runtime.ToString());
                factory.AddSecretClient(new Uri(runtimeKeyVaultUri)).WithName(HostTypeOptions.Runtime.ToString());
                factory.AddKeyClient(new Uri(serviceKeyVaultUri)).WithName(HostTypeOptions.ServiceInstance.ToString());
                factory.AddCertificateClient(new Uri(serviceKeyVaultUri)).WithName(HostTypeOptions.ServiceInstance.ToString());
                factory.AddSecretClient(new Uri(serviceKeyVaultUri)).WithName(HostTypeOptions.ServiceInstance.ToString());

            });

            return services;
        }
    }
}
