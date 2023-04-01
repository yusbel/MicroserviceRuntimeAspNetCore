using Microsoft.Extensions.Logging;
using Sample.Sdk.Data;
using Sample.Sdk.Interface.Security.Symetric;
using System.Security.Cryptography;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;

namespace Sample.Sdk.Core.Security.Symetric
{
    public class AesSymetricCryptoProvider : ISymetricCryptoProvider, IAesKeyRandom
    {
        private const int KeySize = 256;
        private const int NonceSize = 13;
        private const int TagSize = 16;

        private readonly ILogger<AesSymetricCryptoProvider> _logger;

        public AesSymetricCryptoProvider(
            ILogger<AesSymetricCryptoProvider> logger)
        {
            _logger = logger;
        }

        public byte[] GenerateRandomKey(int keySize)
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.GenerateKey();
            return aes.Key;
        }

        public bool TryDecrypt(byte[] cypherText, byte[] key, byte[] tag, byte[] nonce, byte[] aad, out SymetricResult? result)
        {
            try
            {
                using var aesCcm = new AesCcm(key);
                var plainText = new byte[cypherText.Length];
                aesCcm.Decrypt(nonce, cypherText, tag, plainText, aad);
                result = new SymetricResult()
                {
                    Key = new List<byte[]> { key },
                    Nonce = new List<byte[]> { nonce },
                    PlainData = plainText,
                    Aad = aad,
                    Tag = new List<byte[]> { tag.ToArray() },
                    EncryptedData = cypherText
                };
                return true;
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                result = default;
                return false;
            }
        }

        public bool TryEncrypt(byte[] plainText, byte[] additionalAuthData, out SymetricResult? result)
        {
            try
            {
                var key = GenerateRandomKey(KeySize);
                return TryEncrypt(key, plainText, additionalAuthData, out result);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                result = default;
                return false;
            }
        }

        public bool TryEncrypt(byte[] key, byte[] plainText, byte[] aad, out SymetricResult? result)
        {
            using var aesCcm = new AesCcm(key);
            var rng = RandomNumberGenerator.Create();
            var nonce = new byte[NonceSize];
            rng.GetBytes(nonce, 0, NonceSize);
            var cypherText = new byte[plainText.Length];
            var tag = new byte[TagSize];
            aesCcm.Encrypt(nonce, plainText, cypherText, tag, aad);
            result = new SymetricResult()
            {
                Key = new List<byte[]> { key },
                Nonce = new List<byte[]> { nonce },
                Tag = new List<byte[]> { tag.ToArray() },
                Aad = aad,
                EncryptedData = cypherText,
                PlainData = plainText
            };
            return true;
        }
    }
}
