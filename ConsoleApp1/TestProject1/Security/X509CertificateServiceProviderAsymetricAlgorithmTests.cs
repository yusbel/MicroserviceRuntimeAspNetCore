using AutoFixture;
using AutoFixture.AutoMoq;
using Azure.Identity;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Moq;
using Sample.Sdk.Core.Security.Providers.Asymetric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Tests.Security
{
    [TestClass]
    public class X509CertificateServiceProviderAsymetricAlgorithmTests
    {
        //private IOptions<AzureCryptoServiceProviderOptions> _options;
        //private IFixture _fixture;
        [TestInitialize]
        public async Task Init() 
        {
            //_fixture = new Fixture().Customize(new AutoMoqCustomization());
            //_options = _fixture.Freeze<Mock<IOptions<AzureCryptoServiceProviderOptions>>>().Object;
            //_options.Value.KeyVaultUri = new Uri("https://learningkeyvaultyusbel.vault.azure.net/");
            //_options.Value.CertificateNameIdentifier = "HttpMessageAsymetricEncryptorCertificate";
        }

        [TestMethod]
        public async Task GivenValidCertificateThenEncryptAndDecryptDataSucceed() 
        {
            //var rsaProvider = new X509CertificateServiceProviderAsymetricAlgorithm(_options, 
            //   new Azure.Security.KeyVault.Certificates.CertificateClient(_options.Value.KeyVaultUri, new DefaultAzureCredential()));
            //var encryptedData = await rsaProvider.Encrypt(Encoding.UTF8.GetBytes("Hello World"), CancellationToken.None);
            //var plainData = await rsaProvider.Decrypt(encryptedData, CancellationToken.None);
            //Assert.IsTrue(Encoding.UTF8.GetString(plainData) == "Hello World");
        }
    }
}
