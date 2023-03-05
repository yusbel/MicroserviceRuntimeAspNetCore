using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data
{
    public class ExternalMessage : IMessageIdentifier
    {
        public string Key { get; set; }
        public string MsgQueueName { get; set; }
        public string MsgQueueEndpoint { get; set; }
        public string MsgDecryptScope { get; set; }
        public string CorrelationId { get; set; }
        public string Content { get; set; }
        public string Id 
        { 
            get 
            {
                return Key;
            } 
            set 
            {
                Key = value; 
            }
        }
    }
}
