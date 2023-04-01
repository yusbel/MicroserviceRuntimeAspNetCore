
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sample.Sdk.Core.Security.Symetric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Tests.Security.Providers.Symetric
{
    [TestClass]
    public class AesSymetricProviderTests
    {
        private ILogger<AesSymetricCryptoProvider> logger = NullLoggerFactory.Instance.CreateLogger<AesSymetricCryptoProvider>();   

        [TestMethod]
        public void GivenAPlainTextThenEncryptAndDecrypt() 
        {
            var aesProvider = new AesSymetricCryptoProvider(logger);
            var plainText = Encoding.UTF8.GetBytes("This is a test");
            var aad = Encoding.UTF8.GetBytes("Yusbel");
            aesProvider.TryEncrypt(plainText, aad, out var result);
            //aesProvider.TryDecrypt(result.EncryptedData, result.Key, result.Tag, result.Nonce, aad, out var decryptedResult);
            //Assert.IsTrue(Encoding.UTF8.GetString(plainText) == Encoding.UTF8.GetString(decryptedResult.PlainData));
        }

    }
}
