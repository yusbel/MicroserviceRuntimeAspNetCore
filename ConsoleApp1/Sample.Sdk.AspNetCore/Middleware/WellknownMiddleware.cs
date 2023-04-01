using Azure;
using Azure.Security.KeyVault.Certificates;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.AspNetCore.Middleware;
using Sample.Sdk.Azure;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Data.Options;
using Sample.Sdk.Interface.Caching;
using Sample.Sdk.Interface.Security;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Security.Providers.Protocol.Dtos;
using Sample.Sdk.Security.Providers.Protocol.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Middleware
{
    public class WellknownMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WellknownMiddleware> _logger;
        private readonly CertificateClient _certificateClient;
        private readonly IOptions<AzureKeyVaultOptions> _keyVaultOption;
        private readonly IAsymetricCryptoProvider _cryptoProvider;
        private readonly IMemoryCacheState<string, ShortLivedSessionState> _memoryCache;

        public WellknownMiddleware(RequestDelegate next
            , ILogger<WellknownMiddleware> logger
            , CertificateClient certificateClient
            , IOptions<AzureKeyVaultOptions> serviceOption
            , IAsymetricCryptoProvider cryptoProvider
            , IMemoryCacheState<string, ShortLivedSessionState> memoryCache) 
        {
            _next = next;
            _logger = logger;
            _certificateClient = certificateClient;
            _keyVaultOption = serviceOption;
            _cryptoProvider = cryptoProvider;
            _memoryCache = memoryCache;
        }

        public async Task InvokeAsync(HttpContext context) 
        {
            if(!string.IsNullOrEmpty(context.Request.Path) && context.Request.Path == "/Verify") 
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("Processing");
                return;
            }
            if (string.IsNullOrEmpty(context.Request.Path.Value) 
                || !context.Request.Path.Value.StartsWith(@"/Wellknown")) 
            {
                await _next.Invoke(context);
                return;
            }
            _logger.LogInformation($"Processing secure connection session request");
            if (context.Request.Method == "GET" && context.Request.Query["action"] == "publickey")
            {
                await ProcessGetPublicKey(context);
            }
            if (context.Request.Method == "POST")
            {
                await ProcessPostCreateSession(context);
            }
            await _next.Invoke(context);
        }

        private async Task ProcessPostCreateSession(HttpContext context)
        {
            _logger.LogInformation("Creating short lived session state");
            context.Request.EnableBuffering();
            using var ms = new MemoryStream();
            try
            {
                await context.Request.Body.CopyToAsync(ms);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when reading the request");
                await context.Response.CreateFailWellknownEndpoint(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                {
                    Reason = EncryptionDecryptionFail.FailToReadRequest
                });
                return;
            }
            PointToPointSessionDto? session;
            try
            {
                session = JsonSerializer.Deserialize<PointToPointSessionDto>(Encoding.UTF8.GetString(ms.ToArray()));
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Deserialization of message content to {nameof(PointToPointSessionDto)}");
                await context.Response.CreateFailWellknownEndpoint(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                {
                    Reason = EncryptionDecryptionFail.DeserializationFail
                });
                return;
            }
            if (session == null
                    || session.EncryptedSessionIdentifier.Length == 0
                    || session.PublicKey.Length == 0)
            {
                await context.Response.CreateFailWellknownEndpoint(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                {
                    Reason = EncryptionDecryptionFail.DeserializationFail
                });
                return;
            }
            byte[] encryptedSessionIdBase64Bytes;
            try
            {
                encryptedSessionIdBase64Bytes = Convert.FromBase64String(session.EncryptedSessionIdentifier);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Converting encrypted session identifier to base 64 string fail");
                await context.Response.CreateFailWellknownEndpoint(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                {
                    Reason = EncryptionDecryptionFail.Base64StringConvertionFail
                });
                return;
            }
            byte[] publicKeyBase64Bytes;
            try
            {
                publicKeyBase64Bytes = Convert.FromBase64String(session.PublicKey);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Converting to base 64 byte array fail");
                await context.Response.CreateFailWellknownEndpoint(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                {
                    Reason = EncryptionDecryptionFail.Base64StringConvertionFail
                });
                return;
            }
            //Decrypting session id that was encrypted with this service public key
            //(bool wasDecrypted, byte[]? decryptedData, EncryptionDecryptionFail reason) =
            //    await _cryptoProvider.Decrypt(encryptedSessionIdBase64Bytes, CancellationToken.None);
            //if (!wasDecrypted || decryptedData == null)
            //{
            //    await context.Response.CreateFailWellknownEndpoint(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
            //    {
            //        Reason = EncryptionDecryptionFail.DecryptionFail
            //    });
            //    return;
            //}
            //(bool wasEncrypted, byte[]? encryptedData, EncryptionDecryptionFail encryptedReason) =
            //        _cryptoProvider.Encrypt(publicKeyBase64Bytes, decryptedData, CancellationToken.None);
            //if (!wasEncrypted || encryptedData == null)
            //{
            //    await context.Response.CreateFailWellknownEndpoint(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
            //    {
            //        Reason = EncryptionDecryptionFail.EncryptFail
            //    });
            //    return;
            //}
            //var shortLivedSession = new ShortLivedSessionState()
            //{
            //    ExternalPublicKey = session.PublicKey,
            //    PlainSessionIdentifier = Convert.ToBase64String(decryptedData),
            //    EncryptedSessionIdentifier = session.EncryptedSessionIdentifier
            //};
            //_memoryCache.Cache.GetOrCreate<ShortLivedSessionState>(
            //        shortLivedSession.EncryptedSessionIdentifier
            //        , (cacheEntry) =>
            //        {
            //            cacheEntry.SetValue(shortLivedSession);
            //            cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMilliseconds(100));
            //            return shortLivedSession;
            //        });
            context.Response.StatusCode = 200;
            //await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Convert.ToBase64String(encryptedData)));
            return;
        }

        private async Task ProcessGetPublicKey(HttpContext context)
        {
            _logger.LogInformation("Retrieving certificate from {}", _keyVaultOption.Value.DefaultCertificateName);
            Response<KeyVaultCertificateWithPolicy> certificate;
            try
            {
                certificate = await _certificateClient.GetCertificateAsync(
                                _keyVaultOption.Value.DefaultCertificateName
                                , CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when downloading the certificate");
                await context.Response.CreateFailWellknownEndpoint(StatusCodes.Status503ServiceUnavailable, new InValidHttpResponseMessage()
                {
                    Reason = EncryptionDecryptionFail.UnableToGetCertificate
                });
                return;
            };
            if (certificate.Value == null || certificate.Value.Cer == null)
            {
                await context.Response.CreateFailWellknownEndpoint(StatusCodes.Status503ServiceUnavailable, new InValidHttpResponseMessage()
                {
                    Reason = EncryptionDecryptionFail.UnableToGetCertificate
                });
                return;
            }
            var publicKeyWrapper = new PublicKeyWrapper();
            publicKeyWrapper.PublicKey = Convert.ToBase64String(certificate.Value.Cer);
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync(JsonSerializer.Serialize(publicKeyWrapper));
            return;
        }
    }
}
