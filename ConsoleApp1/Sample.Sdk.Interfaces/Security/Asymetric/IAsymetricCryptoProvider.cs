using Sample.Sdk.Data.Enums;
using Sample.Sdk.Interface.Security.Signature;
using static Sample.Sdk.Data.Enums.Enums;

namespace Sample.Sdk.Interface.Security.Asymetric
{
    /// <summary>
    /// Define the asymetric implementation, there would be different implementations to reach the security requirements for the data dependening on use
    /// Public as this inrterface will be injected in other assemblies
    /// </summary>
    public interface IAsymetricCryptoProvider : ISignatureVerifier
    {
        Task<(bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason)> Decrypt(byte[] data,
            HostTypeOptions keyVaultType,
            string certificateName,
            CancellationToken token);
        Task<(bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason)> Encrypt(byte[] data,
            HostTypeOptions keyVaultType,
            string certificateName,
            CancellationToken token);
        (bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason) Encrypt(byte[] publicKey,
            byte[] data,
            CancellationToken token);

        Task<(bool wasValid, EncryptionDecryptionFail reason)>
            VerifySignature(string publicKeyUri,
                            string certificateKey,
                            byte[] hashValue,
                            byte[] baseSignature,
                            CancellationToken token);
    }
}
