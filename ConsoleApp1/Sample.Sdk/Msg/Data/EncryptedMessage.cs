using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data
{
    public class EncryptedMessage
    {
        public string Key { get; init; }
        public string MsgDecryptScope { get; init; }// add to signature, add the scope to the decrypt token
        public string MsgQueueName { get; init; }// add to signture
        public string MsgQueueEndpoint { get;init; }// add to signature
        public string CorrelationId { get; init; }
        public long CreatedOn { get; init; }
        public string EncryptedEncryptionIv { get; init; }
        public string EncryptedEncryptionKey { get; init; }
        public string Signature { get; init; }
        public string WellKnownEndpoint { get; init; } //add to signature
        public string DecryptEndpoint { get; init; } //add to signature
        public string AcknowledgementEndpoint { get; init; } // add to signature
        public string EncryptedContent { get; set; }

    }
}
