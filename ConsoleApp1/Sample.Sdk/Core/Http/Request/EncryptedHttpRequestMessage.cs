using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Http.Request
{
    public class EncryptedHttpRequestMessage : HttpRequestMessage
    {
        public IEnumerable<string> HeadersToEncrypt { get; set; }
        
    }
}
