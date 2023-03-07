using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Extensions
{
    public static class ExternalMessageExtensions
    {
        public static OutgoingEventEntity ConvertToOutgoingEventEntity(this ExternalMessage message, string eventEntityId = "") 
        {
            return new OutgoingEventEntity()
            {
                Id = eventEntityId,
                MessageKey = message.EntityId,
                CreationTime = DateTime.UtcNow.ToLong(),
                IsDeleted = false,
                Type = message.GetType().AssemblyQualifiedName ?? message.GetType().Name,
                Version = "1.0.0",
                MsgQueueName = message.MsgQueueName,
                MsgDecryptScope = message.MsgDecryptScope,
                MsgQueueEndpoint = message.MsgQueueEndpoint
            };
        }
    }
}
