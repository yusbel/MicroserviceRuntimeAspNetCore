using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol.State
{
    public class ShortLivedSessionState
    {
        //With the my public key
        public string EncryptedSessionIdentifier { get; set; }
        public string PlainSessionIdentifier { get; set; }
        public string ExternalPublicKey { get; set; }
        public bool IsEnabled { get; set; }
        //TODO: AddTTL
        public TimeSpan Expiry { get; set; }
    }
}
