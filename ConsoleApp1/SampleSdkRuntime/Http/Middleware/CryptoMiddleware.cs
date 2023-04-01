using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Sample.Sdk.Core.Data;
using Sample.Sdk.Data;
using Sample.Sdk.Data.Enums;
using Sample.Sdk.Data.HttpResponse;
using Sample.Sdk.Interface.Security.Certificate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Http.Middleware
{
    public class CryptoMiddleware
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly ILogger<CryptoMiddleware> _logger;
        private readonly ICertificateProvider _certificateProvider;
        private readonly CancellationTokenSource _tokenSource;

        public CryptoMiddleware(RequestDelegate requestDelegate,
            ILogger<CryptoMiddleware> logger,
            ICertificateProvider certificateProvider)
        {
            _requestDelegate = requestDelegate;
            _logger = logger;
            _certificateProvider = certificateProvider;
            _tokenSource = new CancellationTokenSource();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/Crypto/PublicKey")
                || context.Request.Method.ToLower() != RequestMethod.Get.Method.ToLower())
            {
                await _requestDelegate(context);
                return;
            }
            if (context.Request.Query.Count == 1 &&
                context.Request.Query.First().Key == "key" &&
                context.Request.Query.First().Value.Count == 1)
            {
                await RetrievePublicKey(context.Request.Query.First().Value.ToString(), context);
                return;
            }
            context.Response.StatusCode = 400;
        }

        private async Task RetrievePublicKey(string keyIdentifier, HttpContext context)
        {
            var result = await _certificateProvider.GetCertificate(keyIdentifier,
                                        Enums.HostTypeOptions.ServiceInstance,
                                        _tokenSource.Token).ConfigureAwait(false);
            if (result.WasDownloaded.HasValue && result.WasDownloaded.Value && result.CertificateWithPolicy!.Cer.Length > 0)
            {
                var certificateBase64String = Convert.ToBase64String(result.CertificateWithPolicy.Cer);
                context.Response.StatusCode = 200;
                var response = new ServiceResponse<PublicKeyResponse>()
                {
                    Data = new PublicKeyResponse()
                    {
                        KeyBase64String = certificateBase64String,
                        KeyId = keyIdentifier
                    }
                };
                context.Response.Headers["Content-Type"] = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response)).ConfigureAwait(false);
                return;
            }
            context.Response.StatusCode = 404;
            return;
        }
    }
}
