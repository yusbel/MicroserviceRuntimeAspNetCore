using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SampleSdkRuntime.Azure.Factory;
using SampleSdkRuntime.Azure.Factory.Interfaces;
using SampleSdkRuntime.Azure.Policies;

namespace SampleSdkRuntimeTests
{
    public class BaseTest 
    {
        protected IConfiguration config = null;
        protected ILoggerFactory loggerFactory = null;
        protected IClientOAuthTokenProviderFactory clientTokenCredentialFactory = null;
        protected IGraphServiceClientFactory graphServiceClientFactory = null;
        protected SecretClient secretClient = null;
        protected IArmClientFactory armClientFactory = null;
        protected IKeyVaultPolicyProvider keyVaultPolicyProvider = null;
        public BaseTest() 
        {
            loggerFactory = LoggerFactory.Create(builder => 
            {
                builder.AddFilter(loglevel => 
                {
                    loglevel = LogLevel.Information;
                    return true;
                });
                builder.ClearProviders();
                builder.AddConsole();
            });
            var configSettings = new Dictionary<string, string>();
            configSettings.Add("AzureKeyVault:ResourceId", @"/subscriptions/e2ad7149-754e-4628-a8dc-54a49b116708/resourceGroups/LearningServiceBus-RG/providers/Microsoft.KeyVault/vaults/learningKeyVaultYusbel");
            configSettings.Add("AZURE_CLIENT_ID", "0f691c02-1c41-4783-b54c-22d921db4e16");
            configSettings.Add("AZURE_CLIENT_SECRET", "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x");
            configSettings.Add("AZURE_TENANT_ID", "c8656f45-daf5-42c1-9b29-ac27d3e63bf3");
            configSettings.Add("AzureKeyVault:KeyVaultUri", "https://learningkeyvaultyusbel.vault.azure.net/");
            
            config = (new ConfigurationBuilder())
                            .AddInMemoryCollection(configSettings)
                            .Build();
            clientTokenCredentialFactory = new ClientOAuthTokenProviderFactory(config);
            graphServiceClientFactory = new GraphServiceClientFactory(clientTokenCredentialFactory);

            if (clientTokenCredentialFactory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecret))
            {
                secretClient = new SecretClient(new Uri(config.GetValue<string>("AzureKeyVault:KeyVaultUri")), clientSecret);
                armClientFactory = new ArmClientFactory(clientSecret);
                keyVaultPolicyProvider = new KeyVaultPolicyProvider(armClientFactory, 
                                                    clientTokenCredentialFactory, 
                                                    loggerFactory.CreateLogger<KeyVaultPolicyProvider>());
            }
        }
    }
}
