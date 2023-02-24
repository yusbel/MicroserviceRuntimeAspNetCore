using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Protocol.State
{
    public enum EncryptionDecryptionFail 
    { 
        None, 
        SessionIsInvalid, 
        DeserializationFail, 
        UnableToCreateChannel, 
        MaxReryReached,
        InValidSender,
        UnableToGetCertificate,
        NoPrivateKeyFound,
        NoPublicKey,
        EncryptFail,
        DecryptionFail,
        SignatureCreationFail,
        VerifySignature,
        InValidKeys,
        InValidPublicKey,
        Base64StringConvertionFail,
        InValidAcknowledgementUri,
        FailToReadRequest,
        TaskCancellationWasRequested
    }

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
