using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Azure.ResourceManager.Resources;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;

namespace Sample.Sdk.Core
{
    public abstract class BaseObject<T> where T : BaseObject<T>
    {
        private IMessageBusSender _msgSender;
        private readonly IOptions<CustomProtocolOptions> _protocolOptions;
        private readonly ISymetricCryptoProvider _cryptoProvider;
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;

        public BaseObject(IMessageBusSender senderMessageDurable
            , IOptions<CustomProtocolOptions> protocolOptions
            , ISymetricCryptoProvider cryptoProvider
            , IAsymetricCryptoProvider asymetricCryptoProvider)
        {
            (_msgSender) = (senderMessageDurable);
            _protocolOptions = protocolOptions;
            _cryptoProvider = cryptoProvider;
            _asymetricCryptoProvider = asymetricCryptoProvider;
        }

        protected abstract Task Save<TE>(TE message, bool sendNotification) where TE : ExternalMessage;
        protected abstract Task Save();
        protected abstract void LogMessage();
        protected async Task<EncryptedMessageMetadata> EncryptExternalMessage<TC>(TC toEncrypt) where TC : ExternalMessage
        {
            var plainData = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(toEncrypt));
            if (_cryptoProvider.TryEncrypt(plainData, out var result))
            {
                var key = Convert.ToBase64String(await _asymetricCryptoProvider.Encrypt(
                                                            result.Key
                                                            , CancellationToken.None));
                var iv = Convert.ToBase64String(await _asymetricCryptoProvider.Encrypt(
                                                            result.Iv
                                                            , CancellationToken.None));
                var createdOn = DateTime.Now.Ticks;
                var encryptedContent = Convert.ToBase64String(result.EncryptedData);
                var signature = await _asymetricCryptoProvider.CreateSignature(
                                                Encoding.UTF8.GetBytes($"{key}:{iv}:{createdOn}:{encryptedContent}"));
                var encryptedMsg = new EncryptedMessageMetadata()
                {
                    CorrelationId = toEncrypt.CorrelationId,
                    Key = toEncrypt.Key,
                    CreatedOn = createdOn,
                    WellKnownEndpoint = _protocolOptions.Value.WellknownSecurityEndpoint,
                    DecryptEndpoint = _protocolOptions.Value.DecryptEndpoint,
                    AcknowledgementEndpoint = _protocolOptions.Value.AcknowledgementEndpoint,
                    EncryptedEncryptionIv = iv,
                    EncryptedEncryptionKey = key,
                    Signature = Convert.ToBase64String(signature),
                    EncryptedContent = encryptedContent
                };
                return encryptedMsg;
            }
            
            return null;
        }
    } 
}
