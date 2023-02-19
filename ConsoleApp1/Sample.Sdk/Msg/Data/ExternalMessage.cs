using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data
{
    public class ExternalMessage
    {
        public string Key { get; set; }
        public string CorrelationId { get; set; }
        public string Content { get; set; }
    }
}
