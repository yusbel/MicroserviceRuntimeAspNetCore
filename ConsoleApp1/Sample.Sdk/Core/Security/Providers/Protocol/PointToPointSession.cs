using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol
{
    public class PointToPointSession    
    {
        public string EncryptedSessionIdentifier { get; set; }
        public string PublicKey { get; set; }
    }
}
