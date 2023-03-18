using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Extensions
{
    public static class EncryptedMessageExtensions
    {
        public static string GetPlainSignature(this EncryptedMessage message) 
        {
            return $"{message.MsgQueueEndpoint}" +
                $"{message.MsgQueueName}" +
                $"{message.AcknowledgementEndpoint}" +
                $"{message.DecryptEndpoint}" +
                $"{message.WellknownEndpoint}" +
                $"{message.DoubleCypherPropertyKeyKey}" +
                $"{message.DoubleCypherPropertyNameKey}" +
                $"{message.CypherPropertyNameKey}" +
                $"{message.CypherPropertyValueKey}" +
                $"{message.CypherContentKey}" +
                $"{message.CypherContentValue}" +
                $"{message.NonceKey}" +
                $"{message.NonceValue}" +
                $"{message.DoubleNonceKey}" +
                $"{message.DoubleNonceValue}" +
                $"{message.TagKey}" +
                $"{message.TagValue}" +
                $"{message.DoubleTagKey}" +
                $"{message.DoubleTagValue}"+
                $"{message.CreatedOn}";
        }
    }
}
