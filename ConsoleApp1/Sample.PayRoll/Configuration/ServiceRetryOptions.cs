using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Configuration
{
    public class ServiceRetryOptions
    {
        public int DelayInSeconds { get; set; }
        public int MaxRetries { get; set; }
        public int MaxDelayInSeconds { get; set; }
        public string Mode { get; set; }

    }
}
