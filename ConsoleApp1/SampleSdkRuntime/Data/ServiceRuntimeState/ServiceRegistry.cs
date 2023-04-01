using Sample.Sdk.Core.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Data.ServiceRuntimeState
{
    public class ServiceRegistry
    {
        public string Id { get; set; } = string.Empty;
        public string ServiceInstanceName { get; set; } = string.Empty;
        public string ServiceInstanceId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public long LastHealthCheck { get; set; }
        
    }
}
