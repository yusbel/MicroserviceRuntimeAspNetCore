using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Msg
{
    public class MessageFailed
    {
        public string MessageId { get; set; } = string.Empty;
        public string SendFailedReason { get; set; } = string.Empty;
    }
}
