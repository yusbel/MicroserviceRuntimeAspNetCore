using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security
{
    public class CustomProtocolOptions
    {
        public const string Identifier = "ServiceSdk:Security:CustomProtocol";
        public string WellknownSecurityEndpoint { get; set; } = string.Empty;
        public string DecryptEndpoint { get; set; } = string.Empty;
        public string AcknowledgementEndpoint { get; set; } = string.Empty;
        public int SessionDurationInSeconds { get; set; }
        public string CryptoEndpoint { get; set; } = string.Empty;
        public string SignDataKeyId { get; set; } = string.Empty;
    }
}
