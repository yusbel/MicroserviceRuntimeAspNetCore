using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public interface ISecurePointToPoint
    {
        Task<byte[]> Decrypt(string wellknownSecurityEndpoint
            , string decryptEndpoint
            , byte[] encryptedData
            , IAsymetricCryptoProvider cryptoProvider
            , CancellationToken token);
    }
}