using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Msg
{
    public class InTransitData : Message
    {
        public string CertificateVaultUri { get; set; } = string.Empty;
        public string CertificateKey { get; set; } = string.Empty;
        public string MsgDecryptScope { get; set; } = string.Empty;
        public string MsgQueueName { get; set; } = string.Empty;
        public string MsgQueueEndpoint { get; set; } = string.Empty;
        public string CryptoEndpoint { get; set; } = string.Empty;
        public string SignDataKeyId { get; set; } = string.Empty;
        public string SignatureCertificateKey { get; set; } = string.Empty;
    }
}
