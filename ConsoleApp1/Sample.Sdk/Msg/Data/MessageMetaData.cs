using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data
{
    public class MessageMetaData : Message
    {
        public string CertificateLocation { get; set; }//add to the signature
        public string CertificateKey { get; set; }//add signature
        public string MsgDecryptScope { get; set; }//add to signature, add the scope to the decrypt token
        public string MsgQueueName { get; set; }//add to signture
        public string MsgQueueEndpoint { get; set; }//add to signature
    }
}
