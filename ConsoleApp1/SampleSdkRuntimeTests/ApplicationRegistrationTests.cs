using AutoFixture;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Moq;
using NuGet.Frameworks;
using SampleSdkRuntime.Azure.AppRegistration;
using SampleSdkRuntime.Azure.DataOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntimeTests
{
    [TestClass]
    public class ApplicationRegistrationTests : BaseTest
    {
        private const string appDisplayName = "ServiceIdentifier-0123456789";
        private IApplicationRegistration appRegistration = null;
        
        [TestInitialize]
        public void Init() 
        {
            appRegistration =  new ApplicationRegistration(
                    clientTokenCredentialFactory,
                    graphServiceClientFactory,
                    keyVaultPolicyProvider,
                    secretClient, 
                    null,
                    loggerFactory.CreateLogger<ApplicationRegistration>());
        }

        [TestMethod]
        public async Task GivenServiceIdentifierThenCreateServicePrinciple() 
        {
            (bool wasCreated, Application? app) = await appRegistration.DeleteAndCreate(appDisplayName, CancellationToken.None);
            Assert.IsTrue(wasCreated);
        }

        [TestMethod]
        public async Task DeleteAll() 
        {
            var wasDeleted = await appRegistration.DeleteAll(appDisplayName, CancellationToken.None);
            Assert.IsTrue(wasDeleted);
        }

        [TestMethod]
        public async Task VerifyApplicationServicePrincipleAccount() 
        {
            var verificationResult = await appRegistration.VerifyApplicationSettings(appDisplayName, CancellationToken.None);
            Assert.IsTrue(verificationResult.isValid);
        }
    }
}
