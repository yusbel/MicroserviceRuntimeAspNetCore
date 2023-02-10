using Microsoft.Extensions.Options;
using Sample.EmployeeSubdomain.WebHook.Data;
using Sample.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.WebHook
{
    /// <summary>
    /// Add to di as singleton
    /// </summary>
    public class WebHookConfiguration
    {
        private readonly IOptions<WebHookConfigurationOptions> _webHookOptions;
        private readonly IOptions<WebHookRetryOptions> _retryOptions;
        private readonly HttpClient _httpClient;

        public WebHookConfiguration(IOptions<WebHookConfigurationOptions> webHookOptions
            , IOptions<WebHookRetryOptions> retryOptions
            , IHttpClientFactory httpClientFactory) 
        {
            Guard.ThrowWhenNull(retryOptions, webHookOptions, httpClientFactory);
            _webHookOptions = webHookOptions;
            _retryOptions = retryOptions;
            _httpClient = httpClientFactory.CreateClient("WebHookHttpClient");
        }

        public async Task<bool> Subscribe() 
        {
            _webHookOptions.Value
                .SubscribeToMessageIdentifiers
                .ToList()
                .ForEach(msgIdentifier => 
                {

                });
            return true;
        }
    }
}
