﻿using Sample.Sdk.Data.Msg;
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
                $"{string.Join(",", message.CypherPropertyNameKey)}" +
                $"{string.Join(",", message.CypherPropertyValueKey)}" +
                $"{message.CreatedOn}";
        }
    }
}
