using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.EntityModel
{
    public class MessageHandlingReason
    {
        public enum SendFailedReason 
        { 
            InValidQueueName, 
            InValidSenderEndpoint,
            SendRaisedException
        }
    }
}
