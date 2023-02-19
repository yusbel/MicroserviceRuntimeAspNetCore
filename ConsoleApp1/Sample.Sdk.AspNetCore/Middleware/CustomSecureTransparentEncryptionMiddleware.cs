using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Sample.Sdk;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
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
        private readonly IInMemoryMessageBus<ShortLivedSessionState> _sessions;
        private readonly ILogger<CustomSecureTransparentEncryptionMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly IAsymetricCryptoProvider _cryptoProvider;

        public CustomSecureTransparentEncryptionMiddleware(
            IInMemoryMessageBus<ShortLivedSessionState> messageBus
            , ILoggerFactory loggerFactory
            , RequestDelegate next
            , IAsymetricCryptoProvider cryptoProvider)
        {
            Guard.ThrowWhenNull(messageBus);
            _sessions = messageBus;
            _logger = loggerFactory.CreateLogger<CustomSecureTransparentEncryptionMiddleware>();
            _next = next;
            _cryptoProvider = cryptoProvider;
        }

        public async Task InvokeAsync(HttpContext context) 
        {
            if(context == null || context.Request.Path != "/Decrypt" || context.Request.Method != "POST") 
            {
                await _next(context);
                return;
            }
            _logger.LogInformation($"Processing request");
            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);
            var encryptedData = System.Text.Json.JsonSerializer.Deserialize<EncryptedData>(Encoding.UTF8.GetString(ms.ToArray()));
            if(encryptedData == null ) 
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest; 
                return;
            }
            if (_sessions.TryGet(encryptedData.SessionEncryptedIdentifier, out var shortLivedSessions)) 
            {
                var shortLivedSession = shortLivedSessions.ToList().FirstOrDefault();
                if (shortLivedSession == null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest; 
                    return;
                }
                if (!IsSenderValid(encryptedData, shortLivedSession)) 
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
                var plainData = await _cryptoProvider.Decrypt(
                    Convert.FromBase64String(encryptedData.Encrypted)
                    , CancellationToken.None);

                //Encrypt with external public key
                var contentEncrypted = _cryptoProvider.Encrypt(
                                                    Convert.FromBase64String(shortLivedSession.ExternalPublicKey)
                                                    , plainData
                                                    , CancellationToken.None);
                //Signature with my private key
                var createdOn = DateTime.Now.Ticks;
                var baseSign = $"{encryptedData.SessionEncryptedIdentifier}:{createdOn}:{Convert.ToBase64String(contentEncrypted)}";
                var singnature = await _cryptoProvider.CreateSignature(Encoding.UTF8.GetBytes(baseSign));
                var reponseEncryptedData = new EncryptedData() 
                {
                    CreatedOn= createdOn, 
                    Encrypted = Convert.ToBase64String(contentEncrypted), 
                    SessionEncryptedIdentifier = encryptedData.SessionEncryptedIdentifier,
                    Signature = Convert.ToBase64String(singnature)
                };
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(reponseEncryptedData)));
                return;
            }
            await _next(context);
        }

        private bool IsSenderValid(EncryptedData? encryptedData, ShortLivedSessionState? shortLivedSession)
        {
            var baseSig = $"{encryptedData?.SessionEncryptedIdentifier}:{encryptedData?.CreatedOn}:{encryptedData.Encrypted}";
            return _cryptoProvider.VerifySignature(
                Convert.FromBase64String(shortLivedSession.ExternalPublicKey)
                , Convert.FromBase64String(encryptedData.Signature)
                , Encoding.UTF8.GetBytes(baseSig));
        }
    }
}
