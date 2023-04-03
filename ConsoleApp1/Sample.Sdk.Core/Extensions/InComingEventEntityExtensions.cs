using Sample.Sdk.Data.Entities;
using Sample.Sdk.Data.Msg;

namespace Sample.Sdk.Core.Extensions
{
    public static class InComingEventEntityExtensions
    {
        public static ExternalMessage ConvertToExternalMessage(this InComingEventEntity inComingEventEntity)
        {
            return new ExternalMessage()
            {
                Id = inComingEventEntity.Id,
                CertificateKey = inComingEventEntity.CertificateKey,
                AckQueueName = inComingEventEntity.AckQueueName,
                CertificateVaultUri = inComingEventEntity.CertificateLocation,
                Content = inComingEventEntity.Body,
                CryptoEndpoint = inComingEventEntity.CryptoEndpoint,
                EntityId = inComingEventEntity.Id,
                MsgDecryptScope = inComingEventEntity.MsgDecryptScope,
                MsgQueueEndpoint = inComingEventEntity.MsgQueueEndpoint,
                MsgQueueName = inComingEventEntity.MsgQueueName,
                SignDataKeyId = inComingEventEntity.SignDataKeyId,
                CorrelationId = inComingEventEntity.Id,
                Type = string.Empty
            };
        }
    }
}
