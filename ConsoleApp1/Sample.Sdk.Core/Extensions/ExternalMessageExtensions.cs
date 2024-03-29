﻿using JsonFlatten;
using Newtonsoft.Json.Linq;
using Sample.Sdk.Data.Constants;
using Sample.Sdk.Data.Entities;
using Sample.Sdk.Data.Msg;
using System.Text;

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
                MsgQueueEndpoint = message.MsgQueueEndpoint,
                CertificateKey = message.CertificateKey,
                CertificateLocation = message.CertificateVaultUri,
                AckQueueName = message.AckQueueName
            };
        }

        public static InComingEventEntity ConvertToInComingEventEntity(this ExternalMessage message)
        {
            return new InComingEventEntity()
            {
                Id = message.Id,
                Body = message.Content,
                MessageKey = message.EntityId,
                CreationTime = DateTime.Now.ToLong(),
                IsDeleted = false,
                Scheme = string.Empty,
                Version = "1.0.0",
                WasAcknowledge = false,
                Type = message.Type,
                CertificateKey = message.CertificateKey,
                CertificateLocation = message.CertificateVaultUri,
                MsgDecryptScope = message.MsgDecryptScope,
                MsgQueueEndpoint = message.MsgQueueEndpoint,
                MsgQueueName = message.MsgQueueName,
                WasProcessed = false,
                AckQueueName = message.AckQueueName,
                ServiceInstanceId = Environment.GetEnvironmentVariable(ConfigVar.SERVICE_INSTANCE_NAME_ID)!
            };
        }

        public static Dictionary<byte[], byte[]> ConvertToDictionaryByteArray(this ExternalMessage message)
        {
            var result = new Dictionary<byte[], byte[]>();
            var jsonStr = System.Text.Json.JsonSerializer.Serialize(message);
            var jObj = JObject.Parse(jsonStr);
            var flattenJsonObj = jObj.Flatten();
            foreach (var item in flattenJsonObj)
            {
                result.Add(Encoding.UTF8.GetBytes(item.Key), Encoding.UTF8.GetBytes(item.Value.ToString()!));
            }
            return result;
        }

        public static byte[] GetInTransitAadData(this ExternalMessage message)
        {
            return message.GetAadData();
        }
    }
}
