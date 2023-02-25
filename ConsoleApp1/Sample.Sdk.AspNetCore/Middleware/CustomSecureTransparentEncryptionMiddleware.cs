using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Sample.Sdk;
using Sample.Sdk.AspNetCore.Middleware;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Middleware
{
    public class CustomSecureTransparentEncryptionMiddleware
    {
        private readonly IMemoryCacheState<string, ShortLivedSessionState> _memoryCache;
        private readonly ILogger<CustomSecureTransparentEncryptionMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly IAsymetricCryptoProvider _cryptoProvider;

        public CustomSecureTransparentEncryptionMiddleware(
            IMemoryCacheState<string, ShortLivedSessionState> memoryCache
            , ILoggerFactory loggerFactory
            , RequestDelegate next
            , IAsymetricCryptoProvider cryptoProvider)
        {
            _memoryCache = memoryCache;
            _logger = loggerFactory.CreateLogger<CustomSecureTransparentEncryptionMiddleware>();
            _next = next;
            _cryptoProvider = cryptoProvider;
        }

        public async Task InvokeAsync(HttpContext context) 
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            try
            {
                if (context == null || context.Request.Path != "/Decrypt" || context.Request.Method != "POST")
                {
                    await _next(context);
                    return;
                }
                _logger.LogInformation($"Processing request");
                using var ms = new MemoryStream();
                try
                {
                    await context.Request.Body.CopyToAsync(ms);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "An error ocurred when reading the request");
                    await context.Response.CreateFailTransparentEncryption(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                    {
                        Reason = EncryptionDecryptionFail.FailToReadRequest
                    });
                    return;
                }
                EncryptedData? encryptedData;
                try
                {
                    encryptedData = System.Text.Json.JsonSerializer.Deserialize<EncryptedData>(Encoding.UTF8.GetString(ms.ToArray()));
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "An error ocurred when deserializing encrypted data");
                    await context.Response.CreateFailTransparentEncryption(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                    {
                        Reason = EncryptionDecryptionFail.DeserializationFail
                    });
                    return;
                }
                if (encryptedData == null)
                {
                    await context.Response.CreateFailTransparentEncryption(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                    {
                        Reason = EncryptionDecryptionFail.DeserializationFail
                    });
                    return;
                }
                if (_memoryCache.Cache.TryGetValue<ShortLivedSessionState>(encryptedData.SessionEncryptedIdentifier, out var shortLivedSession))
                {
                    if (!IsSenderValid(encryptedData, shortLivedSession, token))
                    {
                        await context.Response.CreateFailTransparentEncryption(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                        {
                            Reason = EncryptionDecryptionFail.InValidSender,
                            PointToPointSessionIdentifier = encryptedData.SessionEncryptedIdentifier

                        });
                        return;
                    }
                    var result = await _cryptoProvider.Decrypt(
                                                    Convert.FromBase64String(encryptedData.Encrypted)
                                                    , CancellationToken.None);
                    if (!result.wasDecrypted || result.data == null)
                    {
                        await context.Response.CreateFailTransparentEncryption(StatusCodes.Status503ServiceUnavailable, new InValidHttpResponseMessage()
                        {
                            PointToPointSessionIdentifier = encryptedData.SessionEncryptedIdentifier,
                            Reason = result.reason
                        });
                        return;
                    }
                    byte[] externalPublicKeyBase64String;
                    try
                    {
                        externalPublicKeyBase64String = Convert.FromBase64String(shortLivedSession.ExternalPublicKey);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error ocurred converting string to by array");
                        await context.Response.CreateFailTransparentEncryption(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                        {
                            Reason = EncryptionDecryptionFail.Base64StringConvertionFail
                        });
                        return;
                    }
                    //Encrypt with external public key
                    (bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason) contentEncrypted =
                        _cryptoProvider.Encrypt(externalPublicKeyBase64String
                                                , result.data
                                                , CancellationToken.None);
                    if (!contentEncrypted.wasEncrypted || contentEncrypted.data == null)
                    {
                        await context.Response.CreateFailTransparentEncryption(StatusCodes.Status503ServiceUnavailable, new InValidHttpResponseMessage()
                        {
                            Reason = EncryptionDecryptionFail.EncryptFail
                        });
                        return;
                    }
                    string contentEncryptedBase64;
                    try
                    {
                        contentEncryptedBase64 = Convert.ToBase64String(contentEncrypted.data);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error ocurred when converting to base 64 string");
                        throw;
                    }
                    //Signature with my private key
                    var createdOn = DateTime.Now.Ticks;
                    var baseSign = $"{encryptedData.SessionEncryptedIdentifier}:{createdOn}:{contentEncryptedBase64}";
                    (bool wasCreated, byte[]? data, EncryptionDecryptionFail reason) singnature =
                                        await _cryptoProvider.CreateSignature(Encoding.UTF8.GetBytes(baseSign), token);
                    if (!singnature.wasCreated || singnature.data == null)
                    {
                        await context.Response.CreateFailTransparentEncryption(StatusCodes.Status400BadRequest, new InValidHttpResponseMessage()
                        {
                            Reason = EncryptionDecryptionFail.SignatureCreationFail
                        });
                        return;
                    }
                    var reponseEncryptedData = new EncryptedData()
                    {
                        CreatedOn = createdOn,
                        Encrypted = contentEncryptedBase64,
                        SessionEncryptedIdentifier = encryptedData.SessionEncryptedIdentifier,
                        Signature = Convert.ToBase64String(singnature.data)
                    };
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(reponseEncryptedData)));
                    return;
                }
                //Session was not found
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new InValidHttpResponseMessage()
                {
                    PointToPointSessionIdentifier = encryptedData.SessionEncryptedIdentifier,
                    Reason = EncryptionDecryptionFail.SessionIsInvalid
                }));
                return;
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "An error ocurred");
            }
            finally 
            {
                cancellationTokenSource.Dispose();
            }
        }

        private bool IsSenderValid(EncryptedData? encryptedData, ShortLivedSessionState? shortLivedSession, CancellationToken token)
        {
            var baseSig = $"{encryptedData?.SessionEncryptedIdentifier}:{encryptedData?.CreatedOn}:{encryptedData?.Encrypted}";
            (bool wasVerified, EncryptionDecryptionFail reason) result = 
                _cryptoProvider.VerifySignature(Convert.FromBase64String(shortLivedSession.ExternalPublicKey)
                                                , Convert.FromBase64String(encryptedData.Signature)
                                                , Encoding.UTF8.GetBytes(baseSig)
                                                , token);
            return result.wasVerified;
        }
    }
}
