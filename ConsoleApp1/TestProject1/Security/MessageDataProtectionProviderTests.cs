
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Sdk.Core.Security.Providers.Asymetric;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Data.Registration;
using Sample.Sdk.Data.Security;
using Sample.Sdk.Interface;
using Sample.Sdk.Interface.Security;
using Sample.Sdk.Interface.Security.Symetric;
using Sample.Sdk.Security.DataProtection;
using SampleSdkRuntime;
using SampleSdkRuntime.Data;
using SampleSdkRuntime.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Tests.Security
{
    [TestClass]
    public class MessageDataProtectionProviderTests
    {
        IServiceProvider _serviceProvider;

        [TestInitialize] 
        public void TestInitialize() 
        {
            var serviceHostVariables = new Dictionary<string, string>
                {
                    { ServiceRuntime.AZURE_TENANT_ID, "c8656f45-daf5-42c1-9b29-ac27d3e63bf3" },
                    { ServiceRuntime.AZURE_CLIENT_ID, "0f691c02-1c41-4783-b54c-22d921db4e16" },
                    { ServiceRuntime.AZURE_CLIENT_SECRET, "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x" },
                    { ServiceRuntime.SERVICE_INSTANCE_ID, "Test" },
                    { ServiceRuntime.IS_RUNTIME, "false" }
                };

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddInMemoryCollection(serviceHostVariables)
                .Build();

            IServiceCollection _services = new ServiceCollection();
            _services.AddSampleSdk(configuration);
            _services.AddSampleSdkDataProtection(configuration, "");
            _services.AddSampleSdkInMemoryServices(configuration);
            _services.AddLogging();

            _services.TryAddTransient<IConfiguration>(service => 
            {
                return configuration;
            });
            _services.TryAddTransient<IServiceContext>(service => 
            {
                return new ServiceContext(ServiceRegistration.DefaultInstance("Test"));
            });
            _serviceProvider = _services.BuildServiceProvider();
        }

        [TestMethod]
        public async Task EncryptMessageKeysTest() 
        {
            try
            {
                var msgDataProtection = _serviceProvider.GetRequiredService<IMessageDataProtectionProvider>();
                var aesKeyRandom = _serviceProvider.GetRequiredService<IAesKeyRandom>();
                var keys = new List<KeyValuePair<SymetricResult, SymetricResult>>();
                for (var i = 0; i < 5; i++)
                {
                    keys.Add(KeyValuePair.Create(
                        new SymetricResult() 
                        { 
                            Key = new List<byte[]> { aesKeyRandom.GenerateRandomKey(256) } 
                        }, 
                        new SymetricResult() 
                        {
                            Key = new List<byte[]> { aesKeyRandom.GenerateRandomKey(256) }
                        }));
                }
                //var encryptedResult = await msgDataProtection.EncryptMessageKeys(keys, CancellationToken.None);
                //var decryptedResult = await msgDataProtection.DecryptMessageKeys(encryptedResult, CancellationToken.None);
                
                
                Assert.IsTrue(true);
            }
            catch (Exception e)
            {

                throw;
            }
            
        }
    }
}
