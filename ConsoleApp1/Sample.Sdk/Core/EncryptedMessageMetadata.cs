using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core
{
    public class EncryptedMessageMetadata
    {
        public string Key { get; init; }
        public string CorrelationId { get; init; }

        public long CreatedOn { get; init; }

        public string EncryptedEncryptionIv { get; init; }
        public string EncryptedEncryptionKey { get; init; }
        public string Signature { get; init; }
        public string WellKnownEndpoint { get; init; }
        public string DecryptEndpoint { get; init; }
        public string EncryptedContent { get; set; }

    }
}
