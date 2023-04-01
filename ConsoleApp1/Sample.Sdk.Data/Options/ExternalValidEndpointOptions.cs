using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Options
{
    public class ExternalValidEndpointOptions
    {
        public const string SERVICE_SECURITY_VALD_ENDPOINTS_ID = "ServiceSdk:Security:ExternalValidEndpoints";
        public string WellknownSecurityEndpoint { get; set; } = string.Empty;
        public string DecryptEndpoint { get; set; } = string.Empty;
        public string AcknowledgementEndpoint { get; set; } = string.Empty;
    }
}
