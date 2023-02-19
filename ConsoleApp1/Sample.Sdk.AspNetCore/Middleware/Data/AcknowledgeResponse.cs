using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.AspNetCore.Middleware.Data
{
    public class AcknowledgeResponse
    {
        public string PointToPointSessionIdentifier { get; set; }
        public string Description { get; set; }
    }
}
