﻿using JsonFlatten;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                MsgQueueEndpoint = message.MsgQueueEndpoint, 
                CertificateKey = message.CertificateKey, 
                CertificateLocation = message.CertificateVaultUri
            };
        }

        public static Dictionary<byte[], byte[]> ConvertToDictionaryByteArray(this ExternalMessage message) 
        {
            var result = new Dictionary<byte[], byte[]>();
            var jsonStr = System.Text.Json.JsonSerializer.Serialize(message);
            var jObj = JObject.Parse(jsonStr);
            var flattenJsonObj = jObj.Flatten();
            foreach( var item in flattenJsonObj ) 
            {
                result.Add(Encoding.UTF8.GetBytes(item.Key), Encoding.UTF8.GetBytes(item.Value.ToString()));
            }
            return result;
        }

        public static byte[] GetInTransitAadData(this ExternalMessage message) 
        {
            return message.GetAadData();
        }
    }
}
