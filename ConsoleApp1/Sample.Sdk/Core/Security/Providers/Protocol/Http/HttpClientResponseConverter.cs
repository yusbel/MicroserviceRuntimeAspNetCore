using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol.Http
{
    public class HttpClientResponseConverter : IHttpClientResponseConverter
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpClientResponseConverter> _logger;

        public HttpClientResponseConverter(
            HttpClient httpClient
            , ILogger<HttpClientResponseConverter> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<(bool isValid, T? data, TInvalid? invalidResponse)> InvokePost<T, TInvalid>(Uri uri, HttpContent content) where T : class where TInvalid : class
        {
            _logger.LogInformation("Invoking endppoint {}", uri);
            HttpResponseMessage responseMessage;
            try
            {
                responseMessage = await _httpClient.PostAsync(uri, content);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when calling the acknowledgement endpoint");
                return (false, default(T?), default(TInvalid?));
            } 
            if (responseMessage.IsSuccessStatusCode)
            {
                T? data;
                try 
                {
                    data = System.Text.Json.JsonSerializer.Deserialize<T>(await responseMessage.Content.ReadAsStringAsync());
                    return (true, data, default(TInvalid?));
                }
                catch(Exception e) 
                {
                    _logger.LogCritical(e, $"An error occurred when deserializing to {nameof(T)}");
                    return (false, default(T?), default(TInvalid?));
                }
            }
            try
            {
                var invalidResponse = System.Text.Json.JsonSerializer.Deserialize<TInvalid>(await responseMessage.Content.ReadAsStringAsync());
                return (false, default(T), invalidResponse);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"An error ocurred when deserializing the response of type {nameof(TInvalid)}");
                return (false, default, default);
            }
        }
    }
}
