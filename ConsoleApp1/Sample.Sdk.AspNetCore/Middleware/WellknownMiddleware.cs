using Azure;
using Azure.Security.KeyVault.Certificates;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.InMemory;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Middleware
{
    public class WellknownMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CertificateClient _certificateClient;
        private readonly IOptions<AzureKeyVaultOptions> _keyVaultOption;
        private readonly IOptions<List<AzurePrincipleAccount>> _accountOptions;
        private readonly IOptions<List<ServiceBusInfoOptions>> serviceBusOptions;
        private readonly IAsymetricCryptoProvider _cryptoProvider;
        private readonly IInMemoryMessageBus<ShortLivedSessionState> _sessions;

        public WellknownMiddleware(RequestDelegate next
            , ILoggerFactory loggerFactory
            , CertificateClient certificateClient
            , IOptions<AzureKeyVaultOptions> serviceOption
            , IOptions<List<AzurePrincipleAccount>> accountOptions 
            , IAsymetricCryptoProvider cryptoProvider
            , IInMemoryMessageBus<ShortLivedSessionState> sessions) 
        {
            _next = next;
            _loggerFactory = loggerFactory;
            _certificateClient = certificateClient;
            _keyVaultOption = serviceOption;
            _accountOptions = accountOptions;
            _cryptoProvider = cryptoProvider;
            _sessions = sessions;
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
            var logger = _loggerFactory.CreateLogger<WellknownMiddleware>();
            logger.LogInformation($"Processing secure connection session request");
            if (context.Request.Method == "GET" && context.Request.Query["action"] == "publickey") //it work
            {
                logger.LogInformation("Retrieving certificate from {}", _keyVaultOption.Value.KeyVaultCertificateIdentifier);
                var cer = await _certificateClient.GetCertificateAsync(_keyVaultOption.Value.KeyVaultCertificateIdentifier, CancellationToken.None);
                if (cer == null) 
                {
                    throw new ApplicationException("Not found certificate");
                }
                var publicKeyWrapper = new PublicKeyWrapper();
                publicKeyWrapper.PublicKey = Convert.ToBase64String(cer.Value.Cer);
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync(JsonSerializer.Serialize(publicKeyWrapper));
                return;
            }
            if (context.Request.Method == "POST")
            {
                logger.LogInformation("Creating short lived session state");
                context.Request.EnableBuffering();
                using var ms = new MemoryStream();
                await context.Request.Body.CopyToAsync(ms);
                try
                {
                    var pointToPointSession = JsonSerializer.Deserialize<PointToPointSession>(Encoding.UTF8.GetString(ms.ToArray()));
                    if (pointToPointSession == null
                        || pointToPointSession.EncryptedSessionIdentifier.Length == 0
                        || pointToPointSession.PublicKey.Length == 0)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        return;
                    }
                    //Decrypting session id that was encrypted with this service public key
                    var plainIdentifier = await _cryptoProvider.Decrypt(Convert.FromBase64String(pointToPointSession.EncryptedSessionIdentifier), CancellationToken.None);
                    //
                    var encryptedWithExternalPublicKey = _cryptoProvider.Encrypt(Convert.FromBase64String(pointToPointSession.PublicKey), plainIdentifier, CancellationToken.None);
                    var session = new ShortLivedSessionState()
                    {
                        ExternalPublicKey = pointToPointSession.PublicKey,
                        PlainSessionIdentifier = Convert.ToBase64String(plainIdentifier),
                        EncryptedSessionIdentifier = pointToPointSession.EncryptedSessionIdentifier,
                        Expiry = TimeSpan.FromMinutes(20)
                    };
                    _sessions.Add(session.EncryptedSessionIdentifier, session);
                    context.Response.StatusCode = 200;
                    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(Convert.ToBase64String(encryptedWithExternalPublicKey)));
                    return;
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            await _next.Invoke(context);
        }
    }
}
