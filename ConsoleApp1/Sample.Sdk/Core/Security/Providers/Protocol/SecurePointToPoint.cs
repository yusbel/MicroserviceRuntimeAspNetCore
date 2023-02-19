using Azure.Security.KeyVault.Certificates;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.InMemory;
using Sample.Sdk.Msg.Data;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public class SecurePointToPoint : ISecurePointToPoint
    {
        IInMemoryMessageBus<PointToPointChannel> _sessions;
        private readonly IOptions<CustomProtocolOptions> _options;
        private readonly CertificateClient _certificateClient;
        private readonly HttpClient _httpClient;
        private readonly IPointToPointChannel _pointToPointChannel;
        private readonly IExternalServiceKeyProvider _externalServiceKeyProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly AzureKeyVaultOptions _serviceOptions;
        public SecurePointToPoint(
            IInMemoryMessageBus<PointToPointChannel> sessions
            , IOptions<CustomProtocolOptions> options
            , IOptions<AzureKeyVaultOptions> serviceOptions
            , CertificateClient certificateClient
            , HttpClient httpClient
            , IPointToPointChannel pointToPointChannel
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , ILoggerFactory loggerFactory)
        {
            Guard.ThrowWhenNull(sessions, options, certificateClient);
            _sessions = sessions;
            _options = options;
            _certificateClient = certificateClient;
            _httpClient = httpClient;
            _pointToPointChannel = pointToPointChannel;
            _externalServiceKeyProvider = externalServiceKeyProvider;
            _loggerFactory = loggerFactory;
            _serviceOptions = serviceOptions.Value;
        }

        /// <summary>
        /// Only one channel per service. Use existing channel if string.empty mean that the receiver service may have expired the session.
        /// Create a session and invooke decrypt. 
        /// </summary>
        /// <param name="wellknownSecurityEndpoint"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task<byte[]> Decrypt(string wellknownSecurityEndpoint
            , string decryptEndpoint
            , byte[] encryptedData
            , IAsymetricCryptoProvider cryptoProvider
            , CancellationToken token)
        {
            if (string.IsNullOrEmpty(wellknownSecurityEndpoint))
            {
                throw new ApplicationException("Invalid wellknown encryption endpoint");
            }
            var identifier = Convert.ToBase64String(Encoding.UTF8.GetBytes(wellknownSecurityEndpoint));
            PointToPointChannel channel = null;
            if (_sessions.TryGet(identifier, out var channelCollection))
            {
                channel = channelCollection.First();
            }
            if (channel == null)
            {
                channel = await _pointToPointChannel.Create(identifier
                    , _options.Value.WellknownSecurityEndpoint
                    , _certificateClient
                    , _serviceOptions
                    , _httpClient
                    , _externalServiceKeyProvider
                    , _loggerFactory
                    , token);
                _sessions.Add(identifier, channel);
            }
            if (channel != null)
            {
                var plainData = await channel.DecryptContent(decryptEndpoint, encryptedData, _httpClient, cryptoProvider);
                return plainData;
            }
            return new byte[0];
        }

        private PointToPointChannel GetChannel(string identifier) 
        {
            if (_sessions.TryGet(identifier, out var result)) 
            {
                return result.First();
            }
            return null;
        }
        public async Task<PointToPointChannel> GetOrCreate(string identifier) 
        {
            var channel = GetChannel(identifier);
            if (channel != null) 
            {
                return channel;
            }
            channel = await _pointToPointChannel.Create(identifier
                , _options.Value.WellknownSecurityEndpoint
                , _certificateClient
                , _serviceOptions
                , _httpClient
                , _externalServiceKeyProvider
                , _loggerFactory
                , CancellationToken.None);
            _sessions.Add(identifier, channel);
            return channel;
        }
    }
}
