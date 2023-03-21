using Sample.Sdk.Core.Security.Providers.Symetric;

namespace Sample.Sdk.Core.Security.DataProtection
{
    public interface IMessageDataProtectionProvider
    {
        bool TryEncrypt(List<byte[]> keys, List<KeyValuePair<SymetricResult, SymetricResult>> data, byte[] aad,
                                    out List<KeyValuePair<SymetricResult, SymetricResult>> msgProtectionResult);

        bool TryEncrypt(List<KeyValuePair<SymetricResult, SymetricResult>> data, byte[] aad,
                            out List<KeyValuePair<SymetricResult, SymetricResult>> msgProtectionResult);

        //bool TryEncrypt(List<KeyValuePair<SymetricResult, SymetricResult>> data, int keyIndex, byte[] aad,
        //                        out List<KeyValuePair<SymetricResult, SymetricResult>> encryptedSymetricResult);
        bool TryDecrypt(List<KeyValuePair<SymetricResult, SymetricResult>> data, int keyIndex, byte[] aad,
            out List<KeyValuePair<SymetricResult, SymetricResult>> result);

        Task<List<KeyValuePair<SymetricResult, SymetricResult>>> EncryptMessageKeys(
                                            List<KeyValuePair<SymetricResult, SymetricResult>> keys,
                                            CancellationToken token);
        Task<List<KeyValuePair<SymetricResult, SymetricResult>>> DecryptMessageKeys(List<KeyValuePair<SymetricResult, SymetricResult>> encryptedKeys, CancellationToken token);

        //bool TryDecrypt(List<KeyValuePair<byte[], byte[]>> keys,
        //    Dictionary<byte[], byte[]> data,
        //    List<KeyValuePair<byte[], byte[]>> nonces,
        //    List<KeyValuePair<byte[], byte[]>> tags,
        //    byte[] aad,
        //    out Dictionary<SymetricResult, SymetricResult> decryptedData);

    }
}