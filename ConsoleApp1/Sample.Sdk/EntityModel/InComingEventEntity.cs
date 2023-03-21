using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.EntityModel
{
    [Table("InComingEvents")]
    public class InComingEventEntity : Entity, IMessageIdentifier
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
        public string Type { get; set; }
        public string Version { get; set; }
        public string MessageKey { get; set; }
        public string Body { get; set; }
        public long CreationTime { get; set; }
        public bool IsDeleted { get; set; }
        public bool WasAcknowledge { get; set; }
        public bool WasProcessed { get; set; }
        public string ServiceInstanceId { get; set; } = string.Empty;
    }
}
