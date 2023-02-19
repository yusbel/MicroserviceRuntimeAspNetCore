using Sample.EmployeeSubdomain.Messages;
using Sample.EmployeeSubdomain.WebHook;
using Sample.Sdk.Core;
using Sample.Sdk.InMemory;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain
{
    public static class RegisterNotifier
    {
        public static void Register()
        {
            MessageNotifier<EmployeeAdded>.Register(typeof(EmployeeAdded), (msg) =>
            {
                (new InMemoryMessageBus<ExternalMessage>()).Add("EmployeeAdded", msg);
                Console.WriteLine("Employee added notifier");
                Console.WriteLine(msg.GetType());
                return true;
            });
        }

        public static async Task WebHook()
        {
            Console.WriteLine("==============Subscribing employee service to payroll added data==============");
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, WebHookEndpoint.WebHookMessagingUrl);
            request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new WebHookSubscriber() { SenderKey = "EmployeeSubdomain", MessageKey = "PayRollAdded", WebHookUrl = WebHookEndpoint.WebHookEmployeeUrl }));
            request.Content.Headers.Add("Context-Type", "application/json");
            var response = await client.PostAsync(WebHookEndpoint.WebHookMessagingUrl, request.Content);
            Console.WriteLine($"Web hook response from messaging: ${await response.Content.ReadAsStringAsync()}");
            Console.WriteLine("===============================================================================");
        }
    }
}
