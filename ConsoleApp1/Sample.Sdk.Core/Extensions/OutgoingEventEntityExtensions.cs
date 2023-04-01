using Sample.Sdk.Data.Entities;
using Sample.Sdk.Data.Msg;
using System.Text.Json;

namespace Sample.Sdk.Core.Extensions
{
    public static class OutgoingEventEntityExtensions
    {
        public static ExternalMessage? ConvertToExternalMessage(this OutgoingEventEntity eventEntity)
        {
            var encryptedMsg = eventEntity.ConvertToEncryptedMessage();
            if (encryptedMsg == null) { return null; }
            return new ExternalMessage
            {
                Id = eventEntity.Id,
                Content = eventEntity.Body,
                CorrelationId = encryptedMsg.CorrelationId,
                EntityId = encryptedMsg.Key,
                MsgDecryptScope = encryptedMsg.MsgDecryptScope,
                MsgQueueEndpoint = encryptedMsg.MsgQueueEndpoint,
                MsgQueueName = encryptedMsg.MsgQueueName,
                AckQueueName = eventEntity.AckQueueName,
                CertificateKey = eventEntity.CertificateKey,
                CertificateVaultUri = eventEntity.CertificateLocation,
                CryptoEndpoint = eventEntity.CryptoEndpoint,
                SignDataKeyId = eventEntity.SingDataKey
            };
        }

        public static EncryptedMessage? ConvertToEncryptedMessage(this OutgoingEventEntity eventEntity)
        {
            return JsonSerializer.Deserialize<EncryptedMessage>(eventEntity.Body);
        }
    }
}
