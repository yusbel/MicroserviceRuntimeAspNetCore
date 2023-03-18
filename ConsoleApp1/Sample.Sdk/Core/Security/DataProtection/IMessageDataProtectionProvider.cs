using Sample.Sdk.Core.Security.Providers.Symetric;

namespace Sample.Sdk.Core.Security.DataProtection
{
    public interface IMessageDataProtectionProvider
    {
        bool TryEncrypt(List<byte[]> keys, Dictionary<byte[], byte[]> data, byte[] aad,
                            out List<KeyValuePair<SymetricResult, SymetricResult>> msgProtectionResult);

        bool TryEncrypt(Dictionary<byte[], byte[]> data, byte[] aad,
                            out List<KeyValuePair<SymetricResult, SymetricResult>> msgProtectionResult);

        Task<Dictionary<byte[], byte[]>> EncryptMessageKeys(List<KeyValuePair<byte[], byte[]>> keys, CancellationToken token);
        Task<Dictionary<byte[], byte[]>> DecryptMessageKeys(Dictionary<byte[], byte[]> encryptedKeys, CancellationToken token);

        bool TryDecrypt(Dictionary<byte[], byte[]> keys,
            Dictionary<byte[], byte[]> data,
            Dictionary<byte[], byte[]> nonces,
            Dictionary<byte[], byte[]> tags,
            byte[] aad,
            out Dictionary<SymetricResult, SymetricResult> decryptedData);

    }
}