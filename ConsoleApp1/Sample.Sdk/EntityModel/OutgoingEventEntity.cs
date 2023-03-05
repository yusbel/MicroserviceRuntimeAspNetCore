using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Sdk.EntityModel
{
    [Table("ExternalEvents")]
    public class OutgoingEventEntity : Entity
    {
        public string MsgQueueEndpoint { get; set; }
        public string MsgQueueName { get; set; }
        public string MsgDecryptScope { get; set; }
        public string? Scheme { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
        public string MessageKey { get; set; }
        public string Body { get; set; }
        public long CreationTime { get; set; }
        public bool IsDeleted { get; set; }
        public bool WasAcknowledge { get; set; }
    }
}