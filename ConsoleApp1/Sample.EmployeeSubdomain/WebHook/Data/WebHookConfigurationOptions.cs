using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.WebHook.Data
{
    public class WebHookConfigurationOptions
    {
        public string WebHookSubscriptionUrl { get; set; }
        public string WebHookSendMessageUrl { get; set; }
        public string WebHookReceiveMessageUrl { get; set; }
        public IEnumerable<string> SubscribeToMessageIdentifiers { get; set; }
    }
}
