using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data
{
    public class InTransitData : Message
    {
        public string CertificateVaultUri { get; set; } = string.Empty;
        public string CertificateKey { get; set; } = string.Empty;
        public string MsgDecryptScope { get; set; } = string.Empty;
        public string MsgQueueName { get; set; } = string.Empty;
        public string MsgQueueEndpoint { get; set; } = string.Empty;
        public string WellknownEndpoint { get; set; } = string.Empty;
        public string DecryptEndpoint { get; set; } = string.Empty;
        public string AcknowledgementEndpoint { get; set; } = string.Empty;
        public string CryptoEndpoint { get; set; } = string.Empty;
        public string SignDataKeyId { get; set; } = string.Empty;
    }
}
