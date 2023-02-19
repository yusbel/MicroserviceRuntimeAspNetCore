using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public class CustomProtocolOptions
    {
        public const string Identifier = "ServiceSdk:Security:CustomProtocol";
        public string WellknownSecurityEndpoint { get; set; }
        public string DecryptEndpoint { get; set; }
        public int SessionDurationInSeconds { get; set; }

    }
}
