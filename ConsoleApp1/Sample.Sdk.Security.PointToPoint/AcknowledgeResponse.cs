using Sample.Sdk.Security.Providers.Protocol.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Http.Data
{
    public class AcknowledgeResponse
    {
        public string PointToPointSessionIdentifier { get; set; }
        public AcknowledgementResponseType AcknowledgementResponseType { get; set; }
        public string Description { get; set; }
    }
}
