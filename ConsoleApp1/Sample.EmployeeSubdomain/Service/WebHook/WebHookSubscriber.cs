using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Service.WebHook
{
    public class WebHookSubscriber
    {
        public string SenderKey { get; set; }
        public string MessageKey { get; init; }
        public string WebHookUrl { get; init; }
    }
}
