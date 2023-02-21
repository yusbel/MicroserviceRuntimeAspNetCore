using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Symetric
{
    public class AesSymetricCryptoProvider : ISymetricCryptoProvider
    {
        private const int KeySize = 256;
        private readonly ILogger<AesSymetricCryptoProvider> _logger;

        public AesSymetricCryptoProvider(
            ILogger<AesSymetricCryptoProvider> logger) 
        {
            _logger = logger;
        }
        
        public bool TryDecrypt(byte[] data, byte[] key, byte[] iv, out SymetricResult? result)
        {
            try
            {
                using var aesCng = AesCng.Create();
                aesCng.Key = key;
                aesCng.IV = iv;
                var plainData = aesCng.DecryptCbc(data, iv, PaddingMode.PKCS7);
                result = new SymetricResult()
                {
                    Key = key,
                    Iv = iv,
                    PlainData = plainData.ToArray()
                };
                return true;
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Symetric decript fail {e}");
                result = default;
                return false;
            }
        }


        public bool TryEncrypt(byte[] data, out SymetricResult? result)
        {
            try
            {
                using var aesCng = AesCng.Create();
                var key = aesCng.Key;
                var iv = aesCng.IV;
                var encryptedData = aesCng.EncryptCbc(data, iv, PaddingMode.PKCS7);
                result = new SymetricResult()
                {
                    Key = key,
                    Iv = iv,
                    EncryptedData = encryptedData.ToArray()
                };
                return true;
            }
            catch (Exception e)
            {
                _logger.LogCritical("An error occurred when encrypting", e);
                result = default;
                return false;
            }
        }
    }
}
