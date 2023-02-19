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
        Task<byte[]> Decrypt(byte[] data, CancellationToken token);
        Task<byte[]> Encrypt(byte[] data, CancellationToken token);
        byte[] Encrypt(byte[] publicKey, byte[] data, CancellationToken token);
    }
}
