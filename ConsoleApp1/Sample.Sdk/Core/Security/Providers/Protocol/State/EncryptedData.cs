using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol.State
{
    public class EncryptedData
    {
        public string SessionEncryptedIdentifier { get; set; }
        public long CreatedOn { get; set; }
        public string Signature { get; set; }
        public string Encrypted { get; set; }
    }
}
