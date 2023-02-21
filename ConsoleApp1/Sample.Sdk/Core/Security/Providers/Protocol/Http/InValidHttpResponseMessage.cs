using Sample.Sdk.Core.Security.Providers.Protocol.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol.Http
{
    public class InValidHttpResponseMessage
    {
        public string? PointToPointSessionIdentifier { get; init; }
        public EncryptionDecryptionFail Reason { get; init; }
    }
}
