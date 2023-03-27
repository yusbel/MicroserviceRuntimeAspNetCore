
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

namespace Sample.Sdk
{
    /// $Env:AZURE_CLIENT_ID="51df4bce-6532-4345-9be7-5be7af315003"
    /// $Env:AZURE_CLIENT_SECRET="tdm8Q~Cw_e7cLFadttN7Zebacx_kC5Y-0xaWZdv2"
    /// $Env:AZURE_TENANT_ID="c8656f45-daf5-42c1-9b29-ac27d3e63bf3"
    public static class SdkRegisterDependencies
    {
        public static IServiceCollection AddSampleSdk(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ServiceDbContext>();
            services.Configure<DatabaseSettingOptions>(configuration.GetSection(DatabaseSettingOptions.DatabaseSetting));
            services.Configure<MessageSettingsConfigurationOptions>(configuration.GetSection(MessageSettingsConfigurationOptions.SECTION_ID));
            services.Configure<AzureKeyVaultOptions>(configuration.GetSection(AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION));

            services.AddSingleton(serviceProvider => 
            {
                return new HttpClient();
            });

            services.AddTransient<IPublicKeyProvider>(serviceProvider => 
            {
                return new PublicKeyProvider(serviceProvider.GetRequiredService<HttpClient>());
            });

            services.AddTransient<ISendExternalMessage, MessageSenderRealtimeService>();
            //services.AddTransient<IHttpClientResponseConverter, HttpClientResponseConverter>();
            services.AddTransient<IMessageInTransitService, MessageInTransitService>();
            services.AddTransient<IOutgoingMessageProvider, SqlOutgoingMessageProvider>();
            
            services.Configure<CustomProtocolOptions>(configuration.GetSection(CustomProtocolOptions.Identifier));

            services.AddSampleSdkTokenCredentials(configuration);
            services.AddSampleSdkAzureKeyVaultCertificateAndSecretClient(configuration);
            services.AddSampleSdkServiceBusReceiver(configuration);
            services.AddSampleSdkServiceBusSender(configuration);
            services.AddSampleSdkCryptographic();

            return services;
        }

        public static IServiceCollection AddSampleSdkCryptographic(this IServiceCollection services) 
        {
            services.AddSingleton<IMemoryCacheState<string, X509Certificate2>, MemoryCacheState<string, X509Certificate2>>();
            services.AddSingleton<IMemoryCacheState<string, KeyVaultCertificateWithPolicy>, MemoryCacheState<string, KeyVaultCertificateWithPolicy>>();

            services.AddTransient<ISignatureCryptoProvider, SignatureCryptoProvider>();
            //services.AddTransient<IPointToPointSession, PointToPointSession>();
            services.AddTransient<IExternalServiceKeyProvider, ExternalServiceKeyProvider>();
            services.AddTransient<IMessageCryptoService, MessageCryptoService>();
            services.AddTransient<ISecurityEndpointValidator, SecurityEndpointValidator>();
            //services.AddTransient<ISecurePointToPoint, SecurePointToPoint>();
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
            var azureKeyVaultSettings = AzureKeyVaultOptions.Create();
            configuration.GetSection(AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION).Bind(azureKeyVaultSettings);
            services.AddTransient<IMessageDataProtectionProvider, MessageDataProtectionProvider>();
            return services;
        }

        public static IServiceCollection AddSampleSdkInMemoryServices(this IServiceCollection services, IConfiguration config) 
        {
            services.AddHostedService<MessageSenderRealtimeHostedService>();
            services.AddHostedService<MessageReceiverRealtimeHostedService>();

            services.AddTransient<IMessageComputation, ComputeReceivedMessage>();
            services.AddTransient<IMessageRealtimeService, MessageReceiverRealtimeService>();
            services.AddTransient<IMessageRealtimeService, MessageSenderRealtimeService>();
            services.AddTransient<IMessageSender, ServiceBusMessageSender>();
            services.AddTransient<IMessageReceiver, ServiceBusMessageReceiver>();

            services.AddSingleton<IInMemoryMessageBus<PointToPointSession>, InMemoryMessageBus<PointToPointSession>>();
            services.AddSingleton<IMemoryCacheState<string, ShortLivedSessionState>, MemoryCacheState<string, ShortLivedSessionState>>();
            services.AddTransient<IMemoryCacheState<string, string>, MemoryCacheState<string, string>>();
            services.Configure<MemoryCacheOptions>(config);
            services.AddTransient<IMemoryCache, MemoryCache>();
            return services;
        }

        public static IServiceCollection AddSampleSdkAzureKeyVaultCertificateAndSecretClient(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<List<AzureKeyVaultOptions>>(configOptions => 
            {
                configOptions.AddRange(GetAzureKeyVaultOptions(config));
            });
            services.Configure<AzureKeyVaultOptions>(config.GetSection(AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION));
            //list of client certificates
            services.AddTransient(serviceProvider => 
            {
                var keyVaultOptions = GetAzureKeyVaultOptions(config);
                var clientTokenFactory = serviceProvider.GetRequiredService<IClientOAuthTokenProviderFactory>();
                if (clientTokenFactory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecretCredential)) 
                {
                    var certificateClients = new List<KeyValuePair<Enums.AzureKeyVaultOptionsType, CertificateClient>>();
                    foreach (var option in keyVaultOptions)
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
                var options = GetAzureKeyVaultOptions(config);
                var keyVaultOptions = options.First(option => option.Type == Enums.AzureKeyVaultOptionsType.ServiceInstance);
                
                azureClientBuilder.AddSecretClient(new Uri(keyVaultOptions.VaultUri));
                azureClientBuilder.AddKeyClient(new Uri(keyVaultOptions.VaultUri));
                azureClientBuilder.UseCredential(serviceProvider => 
                {
                    var clientCredentialFactory = serviceProvider.GetRequiredService<IClientOAuthTokenProviderFactory>();
                    clientCredentialFactory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecretCredential);
                    return clientSecretCredential;
                });
            });
            return services;
        }

        public static IServiceCollection AddSampleSdkServiceBusSender(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<List<AzureMessageSettingsOptions>>(config.GetSection(AzureMessageSettingsOptions.SENDER_SECTION_ID));

            services.AddAzureClients(azureClientFactory =>
            {
                var senderOptions = new List<AzureMessageSettingsOptions>();
                config.GetSection(AzureMessageSettingsOptions.SENDER_SECTION_ID).Bind(senderOptions);
                azureClientFactory.AddServiceBusClient(senderOptions.First().ConnStr);
            });
            return services;
        }

        public static IServiceCollection AddSampleSdkServiceBusReceiver(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<List<AzureMessageSettingsOptions>>(config.GetSection(AzureMessageSettingsOptions.RECEIVER_SECTION_ID));
            
            services.AddAzureClients(azureFactory => 
            {
                var receiverOptions = new List<AzureMessageSettingsOptions>();
                config.GetSection(AzureMessageSettingsOptions.RECEIVER_SECTION_ID).Bind(receiverOptions);
                azureFactory.AddServiceBusClient(receiverOptions.First().ConnStr);
            });
            
            return services;
        }

        private static List<AzureKeyVaultOptions> GetAzureKeyVaultOptions(IConfiguration configuration) 
        {
            var options = new List<AzureKeyVaultOptions> 
            {
                new AzureKeyVaultOptions{ Type = Enums.AzureKeyVaultOptionsType.Runtime },
                new AzureKeyVaultOptions{ Type = Enums.AzureKeyVaultOptionsType.ServiceInstance }
            };
            configuration.GetSection(AzureKeyVaultOptions.RUNTIME_KEYVAULT_SECTION).Bind(options.First(option => option.Type == Enums.AzureKeyVaultOptionsType.Runtime));
            configuration.GetSection(AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION).Bind(options.First(option=> option.Type == Enums.AzureKeyVaultOptionsType.ServiceInstance));
            return options;
        }
    }
}
