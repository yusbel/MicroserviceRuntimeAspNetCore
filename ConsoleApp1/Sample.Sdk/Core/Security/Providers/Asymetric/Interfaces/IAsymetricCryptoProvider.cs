using Microsoft.Azure.Amqp.Framing;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Symetric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces
{
    /// <summary>
    /// Define the asymetric implementation, there would be different implementations to reach the security requirements for the data dependening on use
    /// Public as this inrterface will be injected in other assemblies
    /// </summary>
    public interface IAsymetricCryptoProvider : ISignatureVerifier
    {
        Task<(bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason)> Decrypt(byte[] data, CancellationToken token);
        Task<(bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason)> Encrypt(byte[] data, CancellationToken token);
        (bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason) Encrypt(byte[] publicKey, byte[] data, CancellationToken token);
    }
}
