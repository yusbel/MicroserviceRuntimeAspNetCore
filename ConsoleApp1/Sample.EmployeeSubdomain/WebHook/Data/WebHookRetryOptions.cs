using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.WebHook.Data
{
    public class WebHookRetryOptions
    {
        public const string SERVICE_WEBHOOK_RETRY_OPTIONS_SECTION_ID = "Employee:WebHookConfiguration:RetryOptions";
        public TimeSpan TimeOutTime 
        { 
            get 
            { 
                return TimeSpan.ParseExact(TimeOut, "hh:mm:ss", CultureInfo.CurrentCulture); 
            } 
        }
        public TimeSpan DelayTime 
        { 
            get 
            { 
                return TimeSpan.ParseExact(Delay, "hh:mm:ss", CultureInfo.CurrentCulture); 
            } 
        }
        public string TimeOut
        {
            get; set;
        } = string.Empty;
        public string Delay { get; set; } = string.Empty;
        public int MaxRetries { get; set; }
    }
}
