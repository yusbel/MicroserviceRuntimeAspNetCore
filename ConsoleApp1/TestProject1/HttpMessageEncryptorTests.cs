using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Options;
using Moq;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestClass]
    public class HttpMessageEncryptorTests
    {
        [TestMethod]
        public async Task GivenValidHttpRequestThenRetrieveEncryptionKeyForHttpRequestMessage() 
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var options = fixture.Freeze<Mock<IOptions<AzureKeyVaultOptions>>>();
            //options.Object.Value.VaultUri = new Uri("https://learningkeyvaultyusbel.vault.azure.net/");
            //var httpEncrytor = new HttpMessageEncryptor(options.Object);
            //var result = await httpEncrytor.Decrypt(new HttpRequestMessage());
            //Assert.IsNotNull(result);
        }
    }
}
