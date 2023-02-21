using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol.State
{
    public class ChannelState
    {
        public TimeSpan Expiry { get; init; }
        public string MyCertWithPrivateKey { get; init; }
        public string MyCertWithPublicKeyOnly { get; init; }
        public string ExternalCertWithPublicKeyOnly { get; init; }
        public byte[] SessionIdentifierEncrypted { get; init; }
        public string SessionIdentifier { get; init; }
        public string Identifier { get; set; }
    }
}
