using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Enums
{
    public class Enums
    {
        public enum SendFailedReason
        {
            InValidQueueName,
            InValidSenderEndpoint,
            SendRaisedException
        }

        public enum StringType { WithPlainDataOnly, WithEncryptedDataOnly, WithoutPlainAndEncryptedData }

        public enum AzureMessageSettingsOptionType { Sender, Receiver }
        public enum HostTypeOptions { Runtime, ServiceInstance }

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
            VerifySignatureFail,
            InValidKeys,
            InValidPublicKey,
            Base64StringConvertionFail,
            InValidAcknowledgementUri,
            FailToReadRequest,
            TaskCancellationWasRequested
        }

    }
}
