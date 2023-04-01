using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Security.Providers.Protocol.State
{
    

    public enum AcknowledgementResponseType
    {
        None,
        ReadingRequestFail,
        DeserializingAcknowledgementFail,
        FromBase64ToByArrayFail,
        RetrieveDbContextFail,
        DeserializationFail,
        SavingToDatabaseFail,
        NoAcknowledgeInDatabase
    }

    public enum TransparentEncrypMiddlewareResponseType
    {
        None,
        ReadingRequestFail,
        DeserializingFail,
        SenderIsInValid
    }
    public class EncryptedData
    {
        public string SessionEncryptedIdentifier { get; set; }
        public long CreatedOn { get; set; }
        public string Signature { get; set; }
        public string Encrypted { get; set; }
    }
}
