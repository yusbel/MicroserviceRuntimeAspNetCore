using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Http.Interfaces;
using Sample.Sdk.Core.Http.Request;
using Sample.Sdk.Core.Security;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Http
{
    /// <summary>
    /// Point to point would use public/private pair of key per service. 
    /// A protocol to stablish the communication will be use by echanging an exncrypted value using their respective public key using a well know endpoint when using transparent encryption.
    /// An Encrypted http request mesaage class will be used to transfer encrypted headers and a header type indicated that its a transaparent encrypted message.
    /// Wellknown and the decrypt endpoint will leverage oauth to authorize access.
    /// TODO:
    /// There is going to be change to this class until i know how to build an effecient maintenable code and use a custom protocol.
    /// Decision chaning as i define the protocol
    /// </summary>
    internal class HttpMessageEncryptor : IHttpMessageEncryptor
    {
        private const string WellknownServiceEndpointHeader = "WellKnownServiceEndpointHeader";
        private const string EncryptedHeaderPrefix = "EncryptedHeather";
        //When true message content and header will be encrypted instead of using a subclass of HttpRequestMessage
        //Message header with prefix are always encrypted and content when not null is always encrypted
        private const string MessageEncrypted = "false";
        private readonly IAsymetricCryptoProvider _cryptoProvider;
        private readonly IOptions<AzureKeyVaultOptions> _serviceOptions;
        private readonly IOptions<CustomProtocolOptions> _protocolOptions;

        public HttpMessageEncryptor(IAsymetricCryptoProvider cryptoProvider
            , IOptions<AzureKeyVaultOptions> serviceOptions
            , IOptions<CustomProtocolOptions> protocolOptions) 
        {
            Guard.ThrowWhenNull(cryptoProvider, serviceOptions);
            _cryptoProvider = cryptoProvider;
            _serviceOptions = serviceOptions;
            _protocolOptions = protocolOptions;
        }
        public async Task<HttpRequestMessage> Decrypt(HttpRequestMessage request, CancellationToken token)
        {
            if (!IsEncriptionOrDecryptionRequired(request)) 
            {
                return request;
            }
            if (request.Content != null) 
            {
                (bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason) plainContent = 
                    await _cryptoProvider.Decrypt(await request.Content.ReadAsByteArrayAsync(), Enums.Enums.AzureKeyVaultOptionsType.Runtime, "", token);
                if (!plainContent.wasDecrypted || plainContent.data == null) 
                {
                    return request;
                }
                request.Content = new ByteArrayContent(plainContent.data);
            }
            //Decrypting headers
            if (request.Headers.Any(h => h.Key.StartsWith(EncryptedHeaderPrefix)))
            {
                request.Headers.Where(h => h.Key.StartsWith(EncryptedHeaderPrefix) && h.Value.Any(hv => hv.Length > 0))
                    .ToList()
                    .ForEach(async header => 
                    {
                        var encryptedStr = string.Join(",", header.Value);
                        (bool wasDecrypt, byte[]? data, EncryptionDecryptionFail reason) plainStr = 
                            await _cryptoProvider.Decrypt(Encoding.UTF8.GetBytes(encryptedStr), Enums.Enums.AzureKeyVaultOptionsType.Runtime, "",  token);
                        if(plainStr.wasDecrypt && plainStr.data != null) 
                        {
                            header.Value.ToList().RemoveAll(h => h.Length > 0);
                            header.Value.ToList().Add(Encoding.UTF8.GetString(plainStr.data));
                        }
                    });
            }
            var headerEncrypted = request.Headers.ToList().FirstOrDefault(h => h.Key == MessageEncrypted);
            if (headerEncrypted.Value != null) 
            {
                headerEncrypted.Value.ToList().RemoveAll(header => header.Length > 0);
                headerEncrypted.Value.ToList().Add("false");
            }
            return request;
        }

        public async Task<HttpResponseMessage> Decrypt(HttpResponseMessage request, CancellationToken token)
        {
            return request;
        }

        /// <summary>
        /// Encrypt content and headers with specified prefix when the message type is an EncryptedHttpRequestMessage
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<HttpRequestMessage> Encrypt(HttpRequestMessage request, CancellationToken token)
        {
            if(!IsEncriptionOrDecryptionRequired(request)) 
            {
                return request;
            }
            var isEncrypted = false;
            var encryptedTypeMsg = (EncryptedHttpRequestMessage)request;
            if (encryptedTypeMsg.Content != null) 
            {
                //(bool wasDecrypted, byte[]? data, EncryptionDecryptionFail reason) encryptedContent = ;
                //    //await _cryptoProvider.Encrypt(await encryptedTypeMsg.Content.ReadAsByteArrayAsync(), default , token);
                //if (!encryptedContent.wasDecrypted || encryptedContent.data == null) 
                //{
                //    return request;
                //}
                //request.Content = new ByteArrayContent(encryptedContent.data);
                isEncrypted = true;
            }
            if(encryptedTypeMsg.Headers.Any() 
                && encryptedTypeMsg.Headers.ToList().Any(h=>h.Key.StartsWith(EncryptedHeaderPrefix))) 
            {
                encryptedTypeMsg.Headers.ToList()
                    .Where(h=> h.Key.StartsWith(EncryptedHeaderPrefix) && h.Value.Any(hv=> hv.Length > 0))
                    .ToList()
                    .ForEach(async header =>
                                    {
                                        var strToEncrypt = String.Join(",", header.Value);
                                        //(bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason) encryptedHeader = 
                                        //    await _cryptoProvider.Encrypt(Encoding.UTF8.GetBytes(strToEncrypt), token);
                                        //if(encryptedHeader.wasEncrypted && encryptedHeader.data != null) 
                                        //{
                                        //    header.Value.ToList().RemoveAll(str => str.Length > 0);
                                        //    header.Value.ToList().Add(Encoding.UTF8.GetString(encryptedHeader.data));
                                        //}
                                    });
                isEncrypted = true;
            }
            if (isEncrypted) 
            {
                request.Headers.Add(WellknownServiceEndpointHeader, _protocolOptions.Value.WellknownSecurityEndpoint);
                request.Headers.Add(MessageEncrypted, "true");
            }
            return request;
        }

        public Task<HttpResponseMessage> Encrypt(HttpResponseMessage request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        private bool IsEncriptionOrDecryptionRequired(HttpRequestMessage request) 
        {
            return (
                request.Headers.Any(h => h.Key == MessageEncrypted 
                && h.Value.Count() == 1 
                && h.Value.Any(hv => hv == "true")));
        }
    }
}
