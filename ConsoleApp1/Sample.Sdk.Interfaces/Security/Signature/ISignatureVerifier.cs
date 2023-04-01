using Sample.Sdk.Data.Enums;
using static Sample.Sdk.Data.Enums.Enums;

namespace Sample.Sdk.Interface.Security.Signature
{
    public interface ISignatureVerifier
    {
        public Task<(bool wasValid, EncryptionDecryptionFail reason)> VerifySignature(byte[] hashValue, byte[] baseSignature,
            HostTypeOptions options,
            string certificateName,
            CancellationToken token);

        public (bool wasValid, EncryptionDecryptionFail reason) VerifySignature(byte[] publicKey, byte[] hashValue, byte[] baseSignature, CancellationToken token);
        public Task<(bool wasCreated, byte[]? data, EncryptionDecryptionFail reason)> CreateSignature(byte[] baseString,
            HostTypeOptions options,
            CancellationToken token);

    }
}
