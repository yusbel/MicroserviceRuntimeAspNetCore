using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol.Dtos
{
    public class PointToPointSessionDto
    {
        public string EncryptedSessionIdentifier { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
    }
}
