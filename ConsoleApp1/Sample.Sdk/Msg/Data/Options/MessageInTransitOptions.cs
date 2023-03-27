using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data.Options
{
    public class MessageInTransitOptions
    {
        public const string SECTION_ID = "Service:AzureMessageSettings:Configuration:MessageInTransitOptions";
        public string MsgQueueName { get; set; } = string.Empty;
        public string MsgDecryptScope { get; set; } = string.Empty;
        public string MsgQueueEndpoint { get; set; } = string.Empty;
        public string AckQueueName { get; set; } = string.Empty;
    }
}
