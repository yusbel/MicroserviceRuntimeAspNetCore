﻿using Azure.Security.KeyVault.Certificates;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sample.Sdk.Azure;
using Sample.Sdk.Core;
using Sample.Sdk.Exceptions;
using Sample.Sdk.InMemory.Interfaces;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Security.Providers.Protocol.Http;
using Sample.Sdk.Security.Providers.Protocol.Interfaces;
using Sample.Sdk.Security.Providers.Protocol.State;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sample.Sdk.Security.Providers.Protocol
{
    public class SecurePointToPoint : ISecurePointToPoint
    {
        IInMemoryMessageBus<PointToPointSession> _sessions;
        private readonly IOptions<CustomProtocolOptions> _options;
        private readonly CertificateClient _certificateClient;
        private readonly IHttpClientResponseConverter _httpClientResponseConverter;
        private readonly HttpClient _httpClient;
        private readonly IPointToPointSession _pointToPointChannel;
        private readonly IExternalServiceKeyProvider _externalServiceKeyProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SecurePointToPoint> _logger;
        private readonly AzureKeyVaultOptions _serviceOptions;
        public SecurePointToPoint(
            IInMemoryMessageBus<PointToPointSession> sessions
            , IOptions<CustomProtocolOptions> options
            , IOptions<AzureKeyVaultOptions> serviceOptions
            , CertificateClient certificateClient
            , IHttpClientResponseConverter httpClientResponseConverter
            , IPointToPointSession pointToPointChannel
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , ILoggerFactory loggerFactory
            , ILogger<SecurePointToPoint> logger)
        {
            Guard.ThrowWhenNull(sessions, options, certificateClient);
            _sessions = sessions;
            _options = options;
            _certificateClient = certificateClient;
            _httpClientResponseConverter = httpClientResponseConverter;
            _pointToPointChannel = pointToPointChannel;
            _externalServiceKeyProvider = externalServiceKeyProvider;
            _loggerFactory = loggerFactory;
            _logger = logger;
            _serviceOptions = serviceOptions.Value;
        }

        /// <summary>
        /// Only one channel per service. Use existing channel if string.empty mean that the receiver service may have expired the session.
        /// Create a session and invooke decrypt. 
        /// </summary>
        /// <param name="wellknownSecurityEndpoint"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task<(bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason)>
            Decrypt(string wellknownSecurityEndpoint
            , string decryptEndpoint
            , byte[] encryptedData
            , IAsymetricCryptoProvider cryptoProvider
            , CancellationToken token)
        {
            if (string.IsNullOrEmpty(wellknownSecurityEndpoint))
                throw new ArgumentNullException(nameof(wellknownSecurityEndpoint));
            if (token.IsCancellationRequested)
                return (false, default, EncryptionDecryptionFail.TaskCancellationWasRequested);

            byte[] wellknowbytes;
            try
            {
                wellknowbytes = Encoding.UTF8.GetBytes(wellknownSecurityEndpoint);
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "failt to convert to byte array");
                return (false, default, EncryptionDecryptionFail.Base64StringConvertionFail);
            }
            var identifier = Convert.ToBase64String(wellknowbytes);
            return await GetPlainData(identifier
                                            , token
                                            , new DecryptContentDependency()
                                            {
                                                CryptoProvider = cryptoProvider,
                                                DecryptEndpoint = decryptEndpoint,
                                                EncryptedData = encryptedData,
                                                ResponseConverter = _httpClientResponseConverter
                                            }
                                            , (false, default(byte[]), EncryptionDecryptionFail.None)
                                            , 0);
        }

        //wasDecrypted = default value false
        private async Task<(bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason)> GetPlainData(
            string sessionIdentifier
            , CancellationToken token
            , DecryptContentDependency decryptContentDependency
            , (bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason) result
            , int counter)
        {
            PointToPointSession? channel = null;
            if (token.IsCancellationRequested)
            {
                result.reason = EncryptionDecryptionFail.TaskCancellationWasRequested;
                return result;
            }
            if (counter == 3)
            {
                result.reason = EncryptionDecryptionFail.MaxReryReached;
                return result;
            }
            if (result.wasDecrypted)
            {
                return result;
            }
            if (counter > 0 && result.wasDecrypted && result.reason == EncryptionDecryptionFail.SessionIsInvalid)
            {
                (bool wasCreated, PointToPointSession? channel) createdChannel =
                    await RemoveAndCreateSessionChannel(sessionIdentifier, token);
                if (createdChannel.wasCreated && createdChannel.channel != null)
                {
                    channel = createdChannel.channel;
                }
            }
            if (counter > 0 && !result.wasDecrypted && result.reason == EncryptionDecryptionFail.DeserializationFail)
            {
                return result;
            }
            if (counter > 0)
            {
                await Task.Delay(1000);
            }
            if (channel == null)
            {
                (bool wasCreated, PointToPointSession? channel) channelCreated =
                    await GetOrCreateSessionChannel(sessionIdentifier, token);
                if (channelCreated.wasCreated && channelCreated.channel != null)
                {
                    channel = channelCreated.channel;
                }
            }
            counter++;
            if (channel != null)
            {
                result = await channel.DecryptContent(
                                    decryptContentDependency.DecryptEndpoint
                                    , decryptContentDependency.EncryptedData
                                    , decryptContentDependency.ResponseConverter
                                    , decryptContentDependency.CryptoProvider
                                    , token);
            }
            else
            {
                result.reason = EncryptionDecryptionFail.UnableToCreateChannel;
            }
            return await GetPlainData(sessionIdentifier, token, decryptContentDependency, result, counter);
        }

        /// <summary>
        /// TODO: replace with memory cache then revew error hadling
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<(bool wasCreated, PointToPointSession? channel)> GetOrCreateSessionChannel(
            string identifier
            , CancellationToken token)
        {
            PointToPointSession? channel;
            if (_sessions.TryGet(identifier, out var channelCollection))
            {
                channel = channelCollection.First();
                return (true, channel);
            }
            try
            {
                (bool wasCreated, PointToPointSession? channel, EncryptionDecryptionFail reason) channelCreated =
                    await _pointToPointChannel.Create(identifier
                                                        , _options.Value.WellknownSecurityEndpoint
                                                        , _certificateClient
                                                        , _serviceOptions
                                                        , _httpClient
                                                        , _externalServiceKeyProvider
                                                        , _loggerFactory
                                                        , token);
                if (!channelCreated.wasCreated || channelCreated.channel == null)
                {
                    return (false, default);
                }
                _sessions.Add(identifier, channelCreated.channel);
                return (true, channelCreated.channel);
            }
            catch (Exception e)
            {
                _loggerFactory.CreateLogger<SecurePointToPoint>().LogCritical(e, "An error occurred when creating secure session");
                return (false, default);
            }
        }

        private async Task<(bool wasCreated, PointToPointSession? channel)> RemoveAndCreateSessionChannel(
            string identifier
            , CancellationToken token)
        {
            _sessions.GetAndRemove(identifier);
            (bool wasCreated, PointToPointSession? channel) createdChannel =
                await GetOrCreateSessionChannel(identifier, token);
            if (!createdChannel.wasCreated || createdChannel.channel == null)
            {
                return (false, default);
            }
            return (true, createdChannel.channel);
        }

        private record DecryptContentDependency
        {
            public string DecryptEndpoint { get; init; }
            public byte[] EncryptedData { get; init; }
            public IHttpClientResponseConverter ResponseConverter { get; init; }
            public IAsymetricCryptoProvider CryptoProvider { get; init; }
        }

    }
}
