using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Sdk.Data.Entities
{
    [Table(name: "ExternalEvents")]
    public class OutgoingEventEntity : Entity
    {
        public string CertificateLocation { get; set; } = string.Empty;
        public string CertificateKey { get; set; } = string.Empty;
        public string MsgQueueEndpoint { get; set; } = string.Empty;
        public string MsgQueueName { get; set; } = string.Empty;
        public string MsgDecryptScope { get; set; } = string.Empty;
        public string? Scheme { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string MessageKey { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public long CreationTime { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsSent { get; set; }
        public string SendFailReason { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 0;
        public string ServiceInstanceId { get; set; } = string.Empty;
        public bool WasAcknowledge { get; set; }
        public string CryptoEndpoint { get; set; } = string.Empty;
        public string SingDataKey { get; set; } = string.Empty;
        public string AckQueueName { get; set; } = string.Empty;


    }
}