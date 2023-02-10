using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.WebHook.Data
{
    public class WebHookRetryOptions
    {
        public TimeSpan TimeOut { get; set; }
        public TimeSpan Delay { get; set; }
        public int MaxRetries { get; set; }
    }
}
