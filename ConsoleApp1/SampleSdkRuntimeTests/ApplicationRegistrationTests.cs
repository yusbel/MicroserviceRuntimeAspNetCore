using AutoFixture;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Castle.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Moq;
using SampleSdkRuntime.Azure.AppRegistration;
using SampleSdkRuntime.Azure.DataOptions;
using SampleSdkRuntime.Azure.Factory;
using SampleSdkRuntime.Azure.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntimeTests
{
    [TestClass]
    public class ApplicationRegistrationTests
    {
        private const string appDisplayName = "ServiceIdentifier-0123456789";
        Dictionary<string,string> _keyValuePairs = new Dictionary<string,string>();
        IClientTokenCredentialFactory clientSecretCredFactory;
        Fixture fixture; 

        [TestInitialize]
        public void Init() 
        {
            var loggerFactory = LoggerFactory.Create(builder => 
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.ClearProviders();
                builder.AddConsole();
            });
            _keyValuePairs.Add("AzureKeyVault:ResourceId", @"/subscriptions/e2ad7149-754e-4628-a8dc-54a49b116708/resourceGroups/LearningServiceBus-RG/providers/Microsoft.KeyVault/vaults/learningKeyVaultYusbel");
            _keyValuePairs.Add("AZURE_CLIENT_ID", "0f691c02-1c41-4783-b54c-22d921db4e16");
            _keyValuePairs.Add("AZURE_CLIENT_SECRET", "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x");
            _keyValuePairs.Add("AZURE_TENANT_ID", "c8656f45-daf5-42c1-9b29-ac27d3e63bf3");

            var keyVaultUri = new Uri("https://learningkeyvaultyusbel.vault.azure.net/");

            fixture = new Fixture();
            fixture.Register<IConfiguration>(() => new ConfigurationBuilder()
                                                            .AddInMemoryCollection(_keyValuePairs)
                                                            .Build());
            fixture.Register<IClientTokenCredentialFactory>(
                () => new ClientTokenCredentialFactory(fixture.Create<IConfiguration>()));
            fixture.Register<IGraphServiceClientFactory>(() => new GraphServiceClientFactory(fixture.Create<IClientTokenCredentialFactory>()));
            
            
            clientSecretCredFactory = fixture.Create<IClientTokenCredentialFactory>();
            if(clientSecretCredFactory.TryGetOrCreateClientSecretCredentialWithDefaultIdentity(out var clientSecret)) 
            {
                fixture.Register<SecretClient>(()=> new SecretClient(keyVaultUri, clientSecret));
                fixture.Register<IArmClientFactory>(() => new ArmClientFactory(clientSecret));
                fixture.Register<IKeyVaultPolicyProvider>(
                                            () => new KeyVaultPolicyProvider(fixture.Create<IArmClientFactory>(), 
                                            fixture.Create<IClientTokenCredentialFactory>(),
                                            loggerFactory.CreateLogger<KeyVaultPolicyProvider>()));
            }
            
            fixture.Register<IApplicationRegistration>(
                () => new ApplicationRegistration(
                    fixture.Create<IClientTokenCredentialFactory>(),
                    fixture.Create<IGraphServiceClientFactory>(),
                    fixture.Create<IKeyVaultPolicyProvider>(),
                    fixture.Create<SecretClient>(), 
                    null,
                    loggerFactory.CreateLogger<ApplicationRegistration>()));
        }

        [TestMethod]
        public async Task GivenServiceIdentifierThenCreateServicePrinciple() 
        {
            var appReg = fixture.Create<IApplicationRegistration>();
            (bool wasCreated, Application? app) = await appReg.GetOrCreate(appDisplayName, CancellationToken.None);
            Assert.IsTrue(wasCreated);
        }

        [TestMethod]
        public async Task DeleteAll() 
        {
            var applicationRegistration = fixture.Create<IApplicationRegistration>();
            var wasDeleted = await applicationRegistration.DeleteAll(appDisplayName, CancellationToken.None);
            Assert.IsTrue(wasDeleted);
        }

        [TestMethod]
        public async Task VerifyApplicationServicePrincipleAccount() 
        {
            var applicationRegistration = fixture.Create<IApplicationRegistration>();
            var verificationResult = await applicationRegistration.VerifyApplicationSettings(appDisplayName, CancellationToken.None);
            Assert.IsTrue(verificationResult.isValid);
        }
    }
}
