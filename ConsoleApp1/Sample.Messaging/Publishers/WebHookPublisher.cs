using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging.Publishers
{
    public class WebHookPublisher : IWebHookPublisher
    {
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
