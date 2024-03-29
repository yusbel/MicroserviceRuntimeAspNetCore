﻿using Azure.Core;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Enums;
using Sample.Sdk.Data.HttpResponse;
using Sample.Sdk.Interface.Azure.BlobLibs;
using Sample.Sdk.Interface.Security.Certificate;
using Sample.Sdk.Interface.Security.Keys;

namespace SampleSdkRuntime.Http.Middleware
{
    public class CryptoMiddleware
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly ILogger<CryptoMiddleware> _logger;
        private readonly ICertificateProvider _certificateProvider;
        private readonly IBlobProvider _blobProvider;
        private readonly CancellationTokenSource _tokenSource;

        public CryptoMiddleware(RequestDelegate requestDelegate,
            ILogger<CryptoMiddleware> logger,
            ICertificateProvider certificateProvider,
            IBlobProvider blobProvider)
        {
            _requestDelegate = requestDelegate;
            _logger = logger;
            _certificateProvider = certificateProvider;
            _blobProvider = blobProvider;
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
            try
            {
                var key = await _blobProvider.DownloadSignaturePublicKey(Environment.GetEnvironmentVariable(ConfigVar.SERVICE_RUNTIME_CERTIFICATE_NAME_APP_CONFIG_KEY)!, CancellationToken.None)
                                                            .ConfigureAwait(false);
                if (key.Length == 0) 
                {
                    var result = await _certificateProvider.GetCertificate(keyIdentifier,
                                                                            Enums.HostTypeOptions.ServiceInstance,
                                                                            _tokenSource.Token)
                                                            .ConfigureAwait(false);
                    if (result.WasDownloaded.HasValue && result.WasDownloaded.Value && result.CertificateWithPolicy!.Cer.Length > 0)
                    {
                        key = result.CertificateWithPolicy.Cer;
                    }
                }
                context.Response.StatusCode = 200;
                var response = new ServiceResponse<PublicKeyResponse>()
                {
                    Data = new PublicKeyResponse()
                    {
                        KeyBase64String = Convert.ToBase64String(key),
                        KeyId = keyIdentifier
                    }
                };

                context.Response.Headers["Content-Type"] = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response)).ConfigureAwait(false);
                return;
            }
            catch (Exception e) { }

            context.Response.StatusCode = 404;
            return;
        }
    }
}
