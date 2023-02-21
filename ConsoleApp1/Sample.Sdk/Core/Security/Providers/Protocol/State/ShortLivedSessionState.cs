using Sample.Sdk.InMemory.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol.State
{
    public class ShortLivedSessionState : CacheEntryState
    {
        public ShortLivedSessionState() 
        {
            AbsoluteExpirationOnSeconds = 600;
            SlidingExpirationOnSeconds = 0;
        }
        public string EncryptedSessionIdentifier { get; set; }
        public string PlainSessionIdentifier { get; set; }
        public string ExternalPublicKey { get; set; }
        public bool IsEnabled { get; set; }
    }
}
