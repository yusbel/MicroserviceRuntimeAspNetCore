using Sample.EmployeeSubdomain.Service.Messages;
using Sample.EmployeeSubdomain.Service.WebHook;
using Sample.Messaging;
using Sample.Sdk.Core;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Service
{
    public static class RegisterNotifier
    {
        public static void Register()
        {
            StaticBaseObject<EmployeeAdded>.Register(typeof(EmployeeAdded), (msg) =>
            {
                InMemmoryMessage<IExternalMessage>.Create().Add("EmployeeAdded", msg);
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
