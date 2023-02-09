using Sample.PayRoll.WebHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll
{
    public static class RegisterNotifier
    {
        public static void Register()
        {
        }

        public static async Task WebHook(PayRollData payrollData)
        {
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, WebHookEndpoint.WebHookMessagingUrl);
            request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payrollData));
            request.Content.Headers.Add("Context-Type", "application/json");
            request.Content.Headers.Add("MessageKey", "PayRollAdded");
            var response = await client.PostAsync(WebHookEndpoint.WebHookMessagingUrl, request.Content);
            Console.WriteLine($"Web hook response from messaging: ${await response.Content.ReadAsStringAsync()}");
        }
    }
}
