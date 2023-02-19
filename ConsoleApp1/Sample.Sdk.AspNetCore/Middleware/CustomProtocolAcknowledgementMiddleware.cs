using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        private readonly IInMemoryMessageBus<ShortLivedSessionState> _shortLivedSessions;
        private readonly IAsymetricCryptoProvider _asymCryptoProvider;
        private readonly ILogger<CustomProtocolAcknowledgementMiddleware> _logger;

        public CustomProtocolAcknowledgementMiddleware(
            RequestDelegate next
            , IProcessAcknowledgement processAcknowledgement
            , IInMemoryMessageBus<ShortLivedSessionState> shortLivedSessions
            , IAsymetricCryptoProvider asymCryptoProvider
            , ILoggerFactory loggerFactory) 
        {
            _next = next;
            _processAcknowledgement = processAcknowledgement;
            _shortLivedSessions = shortLivedSessions;
            _asymCryptoProvider = asymCryptoProvider;
            _logger = loggerFactory.CreateLogger<CustomProtocolAcknowledgementMiddleware>();
        }

        public async Task InvokeAsync(HttpContext context) 
        {
            if(context != null
                && context.Request.Path == "/Acknowledgement"
                && context.Request.Method == "POST") 
            {
                context.Request.EnableBuffering();
                using var ms = new MemoryStream();
                await context.Request.Body.CopyToAsync(ms);
                var ackMsg = System.Text.Json.JsonSerializer.Deserialize<MessageProcessedAcknowledgement>(ms.ToArray());
                //get session
                if (_shortLivedSessions.TryGet(ackMsg.PointToPointSessionIdentifier, out var sessions)) 
                {
                    var session = sessions.First();
                    //valid signature
                    var baseSign = $"{ackMsg.PointToPointSessionIdentifier}:{ackMsg.CreatedOn}:{ackMsg.EncryptedExternalMessage}";
                    var isValidSignature = _asymCryptoProvider.VerifySignature(
                        Convert.FromBase64String(session.ExternalPublicKey)
                        , Convert.FromBase64String(ackMsg.Signature)
                        , Encoding.UTF8.GetBytes(baseSign));
                    if (isValidSignature) 
                    {
                        await _processAcknowledgement.Process(ackMsg);
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        return;
                    }
                    context.Response.StatusCode = StatusCodes.Status400BadRequest; 
                    return;
                }
                _logger.LogError($"Session with identifier {ackMsg.PointToPointSessionIdentifier} was not found");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            await _next(context);
        }
    }
}
