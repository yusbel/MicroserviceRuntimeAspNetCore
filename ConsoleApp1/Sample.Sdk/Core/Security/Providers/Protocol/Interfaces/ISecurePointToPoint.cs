﻿using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;

namespace Sample.Sdk.Core.Security.Providers.Protocol.Interfaces
{
    public interface ISecurePointToPoint
    {
        Task<(bool wasDecrypted, byte[]? data, State.EncryptionDecryptionFail reason)> Decrypt(
            string wellknownSecurityEndpoint
            , string decryptEndpoint
            , byte[] encryptedData
            , IAsymetricCryptoProvider cryptoProvider
            , CancellationToken token);

        Task<(bool wasCreated, PointToPointSession? channel)> GetOrCreateSessionChannel(
            string identifier
            , CancellationToken token);
    }
}