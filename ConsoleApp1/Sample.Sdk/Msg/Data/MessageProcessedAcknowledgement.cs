using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data
{
    public class MessageProcessedAcknowledgement
    {
        public string PointToPointSessionIdentifier { get; set; } = string.Empty;
        public long CreatedOn { get; set; }
        public string Signature { get; set; } = string.Empty;
        public string EncryptedExternalMessage { get; set; } = string.Empty;
    }
}
