using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Sdk.Data.Entities
{
    [Table("InComingEvents")]
    public class InComingEventEntity : Entity
    {
        public string CertificateLocation { get; set; } = string.Empty;
        public string CertificateKey { get; set; } = string.Empty;
        public string MsgQueueEndpoint { get; set; } = string.Empty;
        public string MsgQueueName { get; set; } = string.Empty;
        public string MsgDecryptScope { get; set; } = string.Empty;
        public string WellknownEndpoint { get; init; } = string.Empty;
        public string DecryptEndpoint { get; init; } = string.Empty;
        public string AcknowledgementEndpoint { get; init; } = string.Empty;
        public string? Scheme { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string MessageKey { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public long CreationTime { get; set; }
        public bool IsDeleted { get; set; }
        public bool WasAcknowledge { get; set; }
        public bool WasProcessed { get; set; }
        public string ServiceInstanceId { get; set; } = string.Empty;
        public string AckQueueName { get; set; } = string.Empty;
        public string CryptoEndpoint { get; set; } = string.Empty;
        public string SignDataKeyId { get; set; } = string.Empty;
    }
}
