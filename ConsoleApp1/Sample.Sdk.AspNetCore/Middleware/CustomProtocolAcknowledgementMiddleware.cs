using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Http.Data;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.InMemory;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.AspNetCore.Middleware
{
    public class CustomProtocolAcknowledgementMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IProcessAcknowledgement _processAcknowledgement;
        private readonly IMemoryCacheState<string, ShortLivedSessionState> _memoryCache;
        private readonly IAsymetricCryptoProvider _asymCryptoProvider;
        private readonly ILogger<CustomProtocolAcknowledgementMiddleware> _logger;

        public CustomProtocolAcknowledgementMiddleware(
            RequestDelegate next
            , IProcessAcknowledgement processAcknowledgement
            , IMemoryCacheState<string, ShortLivedSessionState> memoryCache
            , IAsymetricCryptoProvider asymCryptoProvider
            , ILoggerFactory loggerFactory) 
        {
            _next = next;
            _processAcknowledgement = processAcknowledgement;
            _memoryCache = memoryCache;
            _asymCryptoProvider = asymCryptoProvider;
            _logger = loggerFactory.CreateLogger<CustomProtocolAcknowledgementMiddleware>();
        }

        public async Task InvokeAsync(HttpContext context) 
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            try
            {
                if (context != null
                        && context.Request.Path == "/Acknowledgement"
                        && context.Request.Method == "POST")
                {
                    context.Request.EnableBuffering();
                    using var ms = new MemoryStream();
                    try
                    {
                        await context.Request.Body.CopyToAsync(ms);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error occurred when reading the request");
                        await context.Response.CreateFailAcknowledgement(AcknowledgementResponseType.ReadingRequestFail);
                        return;
                    }
                    MessageProcessedAcknowledgement? ackMsg;
                    try
                    {
                        ackMsg = System.Text.Json.JsonSerializer.Deserialize<MessageProcessedAcknowledgement>(ms.ToArray());
                        if (ackMsg == null)
                        {
                            await context.Response.CreateFailAcknowledgement(AcknowledgementResponseType.DeserializingAcknowledgementFail);
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error occurred where deserializing acknowledgement request");
                        await context.Response.CreateFailAcknowledgement(AcknowledgementResponseType.DeserializingAcknowledgementFail);
                        return;
                    }

                    //get session
                    if (_memoryCache.Cache.TryGetValue<ShortLivedSessionState>(ackMsg.PointToPointSessionIdentifier, out var session))
                    {
                        //valid signature
                        var baseSign = $"{ackMsg.PointToPointSessionIdentifier}:{ackMsg.CreatedOn}:{ackMsg.EncryptedExternalMessage}";
                        byte[] publicKey;
                        byte[] signature;
                        (bool isValid, EncryptionDecryptionFail reason) signatureVerification;
                        try
                        {
                            signatureVerification = _asymCryptoProvider.VerifySignature(
                                                            Convert.FromBase64String(session.ExternalPublicKey)
                                                            , Convert.FromBase64String(ackMsg.Signature)
                                                            , Encoding.UTF8.GetBytes(baseSign)
                                                            , cancellationToken);
                        }
                        catch (Exception e)
                        {
                            _logger.LogCritical(e, "An error ocurred when converting to byte array from base 64 string");
                            await context.Response.CreateFailAcknowledgement(AcknowledgementResponseType.FromBase64ToByArrayFail);
                            return;
                        }

                        if (signatureVerification.isValid)
                        {
                            await _processAcknowledgement.Process(ackMsg);
                            context.Response.StatusCode = StatusCodes.Status200OK;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new AcknowledgeResponse()
                            {
                                PointToPointSessionIdentifier = ackMsg.PointToPointSessionIdentifier,
                                Description = String.Empty
                            }));
                            return;
                        }
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new AcknowledgeResponse()
                        {
                            PointToPointSessionIdentifier = ackMsg.PointToPointSessionIdentifier,
                            Description = "Invalid signature"
                        }));
                        return;
                    }
                    _logger.LogError($"Session with identifier {ackMsg.PointToPointSessionIdentifier} was not found");
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new AcknowledgeResponse()
                    {
                        PointToPointSessionIdentifier = ackMsg.PointToPointSessionIdentifier,
                        Description = "Create session and resend"
                    }));
                    return;
                }
                await _next(context);
            }
            catch (Exception e) 
            {
                e.LogException(_logger, "An error ocurred");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }
    }
}
