using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Attributes
{
    public class MessageMetadaAttribute : Attribute
    {
        public MessageMetadaAttribute(string queueName, string decryptScope)
        {
            QueueName = queueName;
            DecryptScope = decryptScope;
        }

        public string QueueName { get; }
        public string DecryptScope { get; }
    }
}
