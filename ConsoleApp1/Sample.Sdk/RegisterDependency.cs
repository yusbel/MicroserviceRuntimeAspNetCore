
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
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Core.Security.Providers.Asymetric;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Signature;
using Sample.Sdk.Core.Security.Providers.Symetric;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.EntityModel;
using Sample.Sdk.InMemory;
using Sample.Sdk.InMemory.InMemoryListMessage;
using Sample.Sdk.InMemory.Interfaces;
using Sample.Sdk.Msg;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Data.Options;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg.Providers;
using Sample.Sdk.Msg.Providers.Interfaces;
using Sample.Sdk.Services;
using Sample.Sdk.Services.Interfaces;
using Sample.Sdk.Services.Realtime;
using Sample.Sdk.Services.Realtime.Interfaces;
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
using Sample.Sdk.Core.Azure.Factory.Interfaces;
using Sample.Sdk.Core.Azure.Factory;
using Azure.Security.KeyVault.Keys.Cryptography;
using Sample.Sdk.Core.Security.DataProtection;
using Sample.Sdk.Core.Security;
using Sample.Sdk.Core.Security.Interfaces;
using System.Net.NetworkInformation;
using Sample.Sdk.Core.Security.Providers.Certificate.Interfaces;
using Sample.Sdk.Core.Security.Providers.Certificate;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;
using Sample.Sdk.Services.Msg;
using Sample.Sdk.Core.Enums;
using Sample.Sdk.Core.Security.Providers;
using Sample.Sdk.Configurations;
using Microsoft.Extensions.Options;

namespace Sample.Sdk
{
    public static class SdkRegisterDependencies
    {
        public static int AzueMessageSettingsOptions { get; private set; }

        public static IServiceCollection AddSampleSdk(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ServiceDbContext>();
            ServiceConfiguration.Create(configuration).AddDatabaseSettingsOptions(services);
            services.Configure<MessageSettingsConfigurationOptions>(configuration.GetSection(MessageSettingsConfigurationOptions.SECTION_ID));
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
            ServiceConfiguration.Create(configuration).AddCustomProtocolOptions(services);
            services.AddSampleSdkTokenCredentials(configuration);
            services.AddSampleSdkAzureKeyVaultCertificateAndSecretClient(configuration);
            services.AddSampleSdkServiceBus(configuration);
            services.AddSampleSdkCryptographic();

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

        public static IServiceCollection AddSampleSdkDataProtection(this IServiceCollection services, IConfiguration configuration, string keyIdentifier)
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

        public static IServiceCollection AddSampleSdkAzureKeyVaultCertificateAndSecretClient(this IServiceCollection services, IConfiguration config) 
        {
            ServiceConfiguration.Create(config).AddAzureKeyVaultOptions(services);
            //list of client certificates
            services.AddTransient(serviceProvider => 
            {
                var keyVaultOptions = serviceProvider.GetRequiredService<IOptions<List<AzureKeyVaultOptions>>>();
                var clientTokenFactory = serviceProvider.GetRequiredService<IClientOAuthTokenProviderFactory>();
                if (clientTokenFactory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecretCredential)) 
                {
                    var certificateClients = new List<KeyValuePair<Enums.AzureKeyVaultOptionsType, CertificateClient>>();
                    foreach (var option in keyVaultOptions.Value)
                    {
                        certificateClients.Add(KeyValuePair.Create(option.Type, 
                            new CertificateClient(new Uri(option.VaultUri), clientSecretCredential)));
                    }
                    return certificateClients;
                }
                throw new InvalidOperationException("Unable to create client secret credential");
            });

            services.AddAzureClients((azureClientBuilder) =>
            {
                var keyVaultUri = ServiceConfiguration.Create(config).GetKeyVaultUri(Enums.AzureKeyVaultOptionsType.ServiceInstance);
                
                azureClientBuilder.AddSecretClient(new Uri(keyVaultUri));
                azureClientBuilder.AddKeyClient(new Uri(keyVaultUri));
                azureClientBuilder.UseCredential(serviceProvider => 
                {
                    var clientCredentialFactory = serviceProvider.GetRequiredService<IClientOAuthTokenProviderFactory>();
                    clientCredentialFactory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecretCredential);
                    return clientSecretCredential;
                });
            });
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
