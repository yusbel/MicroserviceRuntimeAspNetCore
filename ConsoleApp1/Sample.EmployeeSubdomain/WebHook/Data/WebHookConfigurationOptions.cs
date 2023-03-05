using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.WebHook.Data
{
    public class WebHookConfigurationOptions
    {
        public const string SERVICE_WEBHOOK_CONFIG_OPTIONS_SECTION_ID = "Employee:WebHookConfiguration";
        public string WebHookSubscriptionUrl { get; set; } = string.Empty;
        public string WebHookSendMessageUrl { get; set; } = string.Empty;
        public string WebHookReceiveMessageUrl { get; set; } = string.Empty;
        public IEnumerable<string> SubscribeToMessageIdentifiers { get; set; } = Enumerable.Empty<string>();

        public WebHookRetryOptions WebHookRetryOptions { get; set; }
    }
}
