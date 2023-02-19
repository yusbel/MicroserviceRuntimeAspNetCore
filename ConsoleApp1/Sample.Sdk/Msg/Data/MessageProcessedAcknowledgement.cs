using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data
{
    public class MessageProcessedAcknowledgement
    {
        public string PointToPointSessionIdentifier { get; set; }
        public long CreatedOn { get; set; }
        public string Signature { get; set; }
        public string EncryptedExternalMessage { get; set; }
    }
}
