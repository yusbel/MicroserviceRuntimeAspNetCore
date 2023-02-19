﻿using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Attributes;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Symetric;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg
{
    public class ServiceBusMessageReceiver<T> : ServiceBusRoot, IMessageBusReceiver<T> where T : ExternalMessage
    {
        public ServiceBusMessageReceiver(
            IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , IAsymetricCryptoProvider asymCryptoProvider
            , ISymetricCryptoProvider cryptoProvider
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , HttpClient httpClient
            , ISecurePointToPoint securePointToPoint
            , IOptions<AzureKeyVaultOptions> options) : 
            base(serviceBusInfoOptions
                , service
                , asymCryptoProvider
                , cryptoProvider
                , externalServiceKeyProvider
                , httpClient
                , options
                , securePointToPoint)
        {
        }

        public async Task<T> Receive(CancellationToken token
            , Func<ExternalMessage, Task<ExternalMessage>> processBeforeCompleted
            , string queueName = "")
        {
            if(!string.IsNullOrEmpty(queueName)) 
            {
                var msgMetadataAttr = typeof(T).GetCustomAttributes(false).OfType<MessageMetadaAttribute>().FirstOrDefault();
                if(msgMetadataAttr == null) 
                {
                    throw new ApplicationException("Queue name is empty");
                }
                queueName = msgMetadataAttr.QueueName;
            }
            if (!serviceBusReceiver.Any(s=> s.Key.ToLower() == queueName.ToLower())) 
            {
                throw new ApplicationException("No receiver registered for this queue");
            }
            var receiver = serviceBusReceiver.First(s=> s.Key.ToLower() == queueName.ToLower()).Value;
            var message = await receiver.ReceiveMessageAsync(null, token);
            if (message == null || message.ContentType != MsgContentType) 
            {
                await receiver.CompleteMessageAsync(message);
                return null;
            }
            var externalMsg = System.Text.Json.JsonSerializer.Deserialize<ExternalMessage>(Encoding.UTF8.GetString(message.Body.ToMemory().ToArray()));
            var externalMsgMetadata = System.Text.Json.JsonSerializer.Deserialize<EncryptedMessageMetadata>(externalMsg.Content);
            var externalMessage = await GetDecryptedExternalMessage(externalMsgMetadata, asymCryptoProvider, token);

            //await processBeforeCompleted(msgToReturn);
            await receiver.CompleteMessageAsync(message);
            return null;
        }

        
    } 
}
