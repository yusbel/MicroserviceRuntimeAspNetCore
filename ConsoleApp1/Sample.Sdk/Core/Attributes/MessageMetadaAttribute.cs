using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Attributes
{
    public class MessageMetadaAttribute : Attribute
    {
        public MessageMetadaAttribute(string queueName) 
        {
            QueueName = queueName;
        }

        public string QueueName { get; }
    }
}
