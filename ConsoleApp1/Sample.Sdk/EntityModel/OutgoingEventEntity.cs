﻿using Sample.Sdk.Msg.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Sdk.EntityModel
{
    [Table(name: "ExternalEvents")]
    public class OutgoingEventEntity : Entity, IMessageIdentifier
    {
        public string CertificateLocation { get; set; } = string.Empty; //add to the signature
        public string CertificateKey { get; set; } = string.Empty; //add signature
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

        
    }
}