
using Azure.Core.Extensions;
using Azure.Identity;
using Azure.ResourceManager.ServiceBus.Models;
using Azure.Security.KeyVault.Certificates;
using Azure.Extensions.AspNetCore.DataProtection.Keys;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.EntityModel;
using Sample.Sdk.InMemory.InMemoryListMessage;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg.Providers;
using Sample.Sdk.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Azure.Security.KeyVault.Keys.Cryptography;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;
using Sample.Sdk.Core.Security.Providers;
using Microsoft.Extensions.Options;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using static Sample.Sdk.Data.Enums.Enums;
using Sample.Sdk.Security.Interfaces;
using Sample.Sdk.Security.Providers.Protocol.Interfaces;
using Sample.Sdk.Security.Providers.Protocol;
using Sample.Sdk.EntityDatabaseContext;
using Sample.Sdk.Interface.Security;
using Sample.Sdk.Core.Security;
using Sample.Sdk.Interface.Security.Symetric;
using Sample.Sdk.Interface.Security.Certificate;
using Sample.Sdk.Interface.Security.Keys;
using Sample.Sdk.Interface.Caching;
using Sample.Sdk.Core.Caching;
using Sample.Sdk.Core.Security.Signature;
using Sample.Sdk.Core.Security.Asymetric;
using Sample.Sdk.Core.Security.Symetric;
using Sample.Sdk.Core.Security.Certificate;
using Sample.Sdk.Core.Msg;
using Sample.Sdk.Interface.Msg;
using Sample.Sdk.Data.Options;
using Sample.Sdk.Interface.Azure.Factory;
using Sample.Sdk.Core.Azure.Factory;

namespace SampleSdkRuntime.Sdk
{
    public static class SdkRegisterDependencies
    {
        public static int AzueMessageSettingsOptions { get; private set; }

        public static IServiceCollection AddSampleSdk(this IServiceCollection services, IConfiguration configuration)
        {
            ServiceConfiguration.Create(configuration).AddDatabaseSettingsOptions(services);
            ServiceConfiguration.Create(configuration).AddCustomProtocolOptions(services);
            services.Configure<MessageSettingsConfigurationOptions>(configuration.GetSection(MessageSettingsConfigurationOptions.SECTION_ID));
            services.AddDbContext<ServiceDbContext>();
            services.AddSingleton(serviceProvider =>
            {
                return new HttpClient();
            });
            services.AddTransient<IPublicKeyProvider>(serviceProvider =>
            {
                return new PublicKeyProvider(serviceProvider.GetRequiredService<HttpClient>());
            });
            services.AddTransient<ISendExternalMessage, MessageSenderService>();
            services.AddTransient<IMessageInTransitService, MessageInTransitService>();
            services.AddTransient<IOutgoingMessageProvider, SqlOutgoingMessageProvider>();
            services.AddSampleSdkTokenCredentials(configuration);
            services.AddSampleSdkServiceBus(configuration);
            services.AddSampleSdkCryptographic();
            services.AddAzureKeyVaultClients(configuration);

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

        public static IServiceCollection AddSampleSdkCryptographic(this IServiceCollection services)
        {
            services.AddSingleton<IMemoryCacheState<string, X509Certificate2>, MemoryCacheState<string, X509Certificate2>>();
            services.AddSingleton<IMemoryCacheState<string, KeyVaultCertificateWithPolicy>, MemoryCacheState<string, KeyVaultCertificateWithPolicy>>();
            services.AddTransient<ISignatureCryptoProvider, SignatureCryptoProvider>();
            services.AddTransient<IExternalServiceKeyProvider, ExternalServiceKeyProvider>();
            services.AddTransient<IMessageCryptoService, MessageCryptoService>();
            services.AddTransient<ISecurityEndpointValidator, SecurityEndpointValidator>();
            services.AddTransient<ISymetricCryptoProvider, AesSymetricCryptoProvider>();
            services.AddTransient<IAesKeyRandom, AesSymetricCryptoProvider>();
            services.AddTransient<IAsymetricCryptoProvider, X509CertificateServiceProviderAsymetricAlgorithm>();
            services.AddTransient<ICertificateProvider, AzureKeyVaultCertificateProvider>();

            return services;
        }

        public static IServiceCollection AddSampleSdkTokenCredentials(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IClientOAuthTokenProviderFactory, ClientOAuthTokenProviderFactory>();
            return services;
        }

        public static IServiceCollection AddSampleSdkDataProtection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IMessageDataProtectionProvider, MessageDataProtectionProvider>();
            return services;
        }

        public static IServiceCollection AddSampleSdkInMemoryServices(this IServiceCollection services, IConfiguration config)
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

        public static IServiceCollection AddSampleSdkServiceBus(this IServiceCollection services, IConfiguration configuration)
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

    }
}
