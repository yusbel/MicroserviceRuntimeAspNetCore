using Sample.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging.Publishers
{
    public class WebHookPublisher : IWebHookPublisher
    {
        private readonly HttpClient _client;

        public WebHookPublisher(HttpClient client)
        {
            Guard.ThrowWhenNull(client);
            _client = client;
        }

        /// <summary>
        /// Lunch concurrent tasks with httpclient using different circuit breaker state
        /// </summary>
        /// <returns></returns>
        public Task<bool> Flush() 
        {

            return Task.FromResult(true);   
        }               

        public async Task<bool> Publish(string webHookUrl, string message)
        {
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, webHookUrl);
            request.Content = new StringContent(message);
            //request.Content.Headers.Add("Content-Type", "application/json");
            await client.PostAsync(webHookUrl, request.Content);
            return true;
        }
    }
}
