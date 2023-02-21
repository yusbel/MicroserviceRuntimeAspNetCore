using Sample.Sdk.Core.Security.Providers.Protocol.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Http.Data
{
    public class AcknowledgeResponse
    {
        public string PointToPointSessionIdentifier { get; set; }
        public AcknowledgementResponseType AcknowledgementResponseType { get; set; }
        public string Description { get; set; }
    }
}
