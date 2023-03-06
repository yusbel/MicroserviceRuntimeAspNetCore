using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.EntityModel;
using Sample.Sdk.InMemory.InMemoryListMessage;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg.Providers;
using Sample.Sdk.Services.Realtime.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sample.Sdk.EntityModel.MessageHandlingReason;

namespace Sample.Sdk.Services.Realtime
{
    public class MessageSenderRealtimeService : IMessageRealtimeService
    {
        private readonly ILogger<MessageSenderRealtimeService> _logger;
        private readonly IInMemoryDeDuplicateCache<ExternalMessageInMemoryList, ExternalMessage> _eventListToSend;
        private readonly IInMemoryCollection<ExternalMessageSentIdInMemoryList, string> _eventListSent;
        private readonly IInMemoryCollection<MessageSentFailedIdInMemmoryList, MessageFailed> _failedEventList;
        private readonly IMessageSender _messageSender;
        private readonly IOutgoingMessageProvider _outgoingMessageProvider;

        public MessageSenderRealtimeService(ILogger<MessageSenderRealtimeService> logger,
            IInMemoryDeDuplicateCache<ExternalMessageInMemoryList, ExternalMessage> eventListToSend,
            IInMemoryCollection<ExternalMessageSentIdInMemoryList, string> eventListSent,
            IInMemoryCollection<MessageSentFailedIdInMemmoryList, MessageFailed> failedEventList,
            IMessageSender messageSender,
            IOutgoingMessageProvider outgoingMessageProvider)
        {
            _logger = logger;
            _eventListToSend = eventListToSend;
            _eventListSent = eventListSent;
            _failedEventList = failedEventList;
            _messageSender = messageSender;
            _outgoingMessageProvider = outgoingMessageProvider;
        }

        /// <summary>
        /// Compute the tasks out of order
        /// </summary>
        /// <param name="cancellationToken">Cancel operation</param>
        /// <returns></returns>
        public async Task Compute(CancellationToken cancellationToken)
        {
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = tokenSource.Token;
            var tasks = new List<Task>();
            tasks.AddTaskWithConfigureAwaitFalse(
                        SendMessage(token),
                        ReadEventFromDurableStorage(token),
                        SaveFailedMessage(token),
                        UpdateOutgoingEventEntity(token));
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
            }
            try 
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }
            catch(Exception e) 
            {
                e.LogException(_logger.LogCritical);
            }
            
        }

        /// <summary>
        /// Send message from event list to send using message sender service.
        /// </summary>
        /// <param name="token">Cancel operation</param>
        /// <returns></returns>
        private async Task SendMessage(CancellationToken token) 
        {
            while (!token.IsCancellationRequested && _eventListToSend.TryTakeAll(out var eventListToSend)) 
            {
                eventListToSend.RemoveAll(e => e == null);
                await Parallel.ForEachAsync(eventListToSend, async (eventEntity, token) =>
                {
                    await _messageSender.Send(token, eventEntity!, 
                                    msg =>
                                    {
                                        //success send
                                        if (msg != null) 
                                        {
                                            _eventListSent.TryAdd(msg.Id);
                                        }
                                    }, (msg, reason, exception)=> 
                                    {
                                        //error on send
                                        if(msg != null && exception == null) 
                                        {
                                            _failedEventList.Add(new MessageFailed()
                                            {
                                                MessageId = msg.Id,
                                                SendFailedReason = reason?.ToString() ?? string.Empty
                                            });
                                        }
                                        if (msg != null && (exception is ServiceBusException)) 
                                        {
                                            _eventListToSend.TryAdd(msg);
                                        }
                                    }).ConfigureAwait(false);

                }).ConfigureAwait(false);
                await Task.Delay(1000, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Read from durable storage events saved to be send that has not being sent or failed before.
        /// </summary>
        /// <param name="token">To cancel operation</param>
        /// <returns></returns>
        private async Task ReadEventFromDurableStorage(CancellationToken token) 
        {
            while (!token.IsCancellationRequested) 
            {
                try
                {
                    var msgs = await _outgoingMessageProvider.GetMessages(token,
                        (eventEntity)=> !eventEntity.IsDeleted && !eventEntity.IsSent && eventEntity.RetryCount == 0)
                        .ConfigureAwait(false);
                    msgs.ToList().ForEach(e =>
                    {
                        _eventListToSend.TryAdd(e);
                    });
                }
                catch (Exception e) 
                {
                    e.LogException(_logger.LogCritical);
                }
                await Task.Delay(TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Read failed list of event to save to durable storage
        /// </summary>
        /// <param name="token">Cancel the operation</param>
        /// <returns></returns>
        private async Task SaveFailedMessage(CancellationToken token) 
        {
            while (!token.IsCancellationRequested && _failedEventList.TryTakeAll(out var msgs)) 
            {
                if (msgs.Any()) 
                {
                    var messageFaileds = new List<string>();
                    msgs.ForEach(msg=> messageFaileds.Add(msg.MessageId));
                    await _outgoingMessageProvider.UpdateSentMessages(messageFaileds, token,
                        entity =>
                        {
                            var msgFailed = msgs.FirstOrDefault(msg => msg.MessageId == entity.Id);
                            if (msgFailed != null)
                            {
                                entity.SendFailReason = msgFailed.SendFailedReason;
                            }
                            entity.RetryCount = entity.RetryCount++;
                            return entity;
                        },
                        (messageId, exception) => 
                        {
                            //on error
                            if(messageId != null && 
                                    (exception is Microsoft.EntityFrameworkCore.DbUpdateException || 
                                    exception is OperationCanceledException)) 
                            {
                                var msgFailed = msgs.FirstOrDefault(msg => msg.MessageId == messageId);
                                if(msgFailed != null) 
                                {
                                    _failedEventList.Add(msgFailed);
                                }
                            }
                        })
                        .ConfigureAwait(false);
                }
                await Task.Delay(TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Update durable storage with message identifier sent
        /// </summary>
        /// <param name="token">To cancel operation</param>
        /// <returns></returns>
        private async Task UpdateOutgoingEventEntity(CancellationToken token) 
        {
            while (!token.IsCancellationRequested && _eventListSent.TryTakeAll(out var listToUpdate)) 
            {
                await _outgoingMessageProvider.UpdateSentMessages(listToUpdate!, token, 
                    (eventEntity) => 
                    {
                        eventEntity.IsSent = true;
                        return eventEntity;
                    },
                    (failMsgId, exception) => 
                    {
                        if (failMsgId != null 
                            && (exception is Microsoft.EntityFrameworkCore.DbUpdateException 
                                || exception is OperationCanceledException)) 
                        {
                            _eventListSent.TryAdd(failMsgId);
                        }
                    })
                    .ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
            }
        }
    }
}
