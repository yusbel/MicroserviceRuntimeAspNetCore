using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security
{
    public class ExternalValidEndpointOptions
    {
        public const string Identifier = "ServiceSdk:Security:ExternalValidEndpoints";
        public string WellknownSecurityEndpoint { get; set; }
        public string DecryptEndpoint { get; set; }
        public string AcknowledgementEndpoint { get; set; }
    }
}
