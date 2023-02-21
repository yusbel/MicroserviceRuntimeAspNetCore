using Microsoft.AspNetCore.Http;
using Sample.Sdk.Core.Http.Data;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.AspNetCore.Middleware
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task CreateFailAcknowledgement(this HttpResponse response, AcknowledgementResponseType acknowledgementResponseType) 
        {
            response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            response.ContentType = "application/json";
            await response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new AcknowledgeResponse()
            {
                AcknowledgementResponseType = acknowledgementResponseType
            }));
        }

        public static async Task CreateFailTransparentEncryption(this HttpResponse response, int statusCode, InValidHttpResponseMessage inValidResponse) 
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            await response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(inValidResponse));
        }

        public static async Task CreateFailWellknownEndpoint(this HttpResponse response, int statusCode, InValidHttpResponseMessage inValidResponse)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            await response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(inValidResponse));
        }
    }
}
