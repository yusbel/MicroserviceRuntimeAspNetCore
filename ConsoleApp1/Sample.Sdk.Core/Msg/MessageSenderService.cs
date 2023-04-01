using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Data.Msg;
using Sample.Sdk.Interface.Caching;
using Sample.Sdk.Interface.Msg;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;

namespace Sample.Sdk.Core.Msg
{
    public class MessageSenderService : IMessageRealtimeService, ISendExternalMessage
    {
        private readonly ILogger<MessageSenderService> _logger;

        private static Lazy<IInMemoryCollection<ExternalMessage>> eventListToSend = new Lazy<IInMemoryCollection<ExternalMessage>>(
            () =>
            {
                return new InMemoryCollection<ExternalMessage>();
            }, true);

        private static Lazy<IInMemoryCollection<string>> eventListSent = new Lazy<IInMemoryCollection<string>>(
            () =>
            {
                return new InMemoryCollection<string>();
            }, true);

        private static Lazy<IInMemoryCollection<MessageFailed>> failedMessage = new Lazy<IInMemoryCollection<MessageFailed>>(
            () =>
            {
                return new InMemoryCollection<MessageFailed>();
            }, true);

        private readonly IInMemoryCollection<ExternalMessage> _eventListToSend = eventListToSend.Value;
        private readonly IInMemoryCollection<string> _eventListSent = eventListSent.Value;
        private readonly IInMemoryCollection<MessageFailed> _failedEventList = failedMessage.Value;
        private readonly IMessageSender _messageSender;
        private readonly IOutgoingMessageProvider _outgoingMessageProvider;

        public MessageSenderService(ILogger<MessageSenderService> logger,
            IMessageSender messageSender,
            IOutgoingMessageProvider outgoingMessageProvider)
        {
            _logger = logger;
            _messageSender = messageSender;
            _outgoingMessageProvider = outgoingMessageProvider;
        }

        public void SendMessage(ExternalMessage externalMessage)
        {
            _eventListToSend.Add(externalMessage);
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
            catch (Exception e)
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
            while (!token.IsCancellationRequested)
            {
                if (_eventListToSend.TryTakeAll(out var eventListToSend))
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
                                        }, (msg, reason, exception) =>
                                        {
                                            //error on send
                                            if (msg != null && exception == null)
                                            {
                                                _failedEventList.Add(new MessageFailed()
                                                {
                                                    MessageId = msg.Id,
                                                    SendFailedReason = reason?.ToString() ?? string.Empty
                                                });
                                            }
                                            if (msg != null && exception is ServiceBusException)
                                            {
                                                _eventListToSend.TryAdd(msg);
                                            }
                                        }).ConfigureAwait(false);

                    }).ConfigureAwait(false);
                }

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
                        (eventEntity) => !eventEntity.IsDeleted && !eventEntity.IsSent && eventEntity.RetryCount == 0)
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
            while (!token.IsCancellationRequested)
            {
                if (_failedEventList.TryTakeAll(out var msgs))
                {
                    if (msgs.Any())
                    {
                        var messageFaileds = new List<string>();
                        msgs.ForEach(msg => messageFaileds.Add(msg.MessageId));
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
                                if (messageId != null &&
                                        (exception is Microsoft.EntityFrameworkCore.DbUpdateException ||
                                        exception is OperationCanceledException))
                                {
                                    var msgFailed = msgs.FirstOrDefault(msg => msg.MessageId == messageId);
                                    if (msgFailed != null)
                                    {
                                        _failedEventList.Add(msgFailed);
                                    }
                                }
                            })
                            .ConfigureAwait(false);
                    }
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
            while (!token.IsCancellationRequested)
            {
                if (_eventListSent.TryTakeAll(out var listToUpdate))
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
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), token).ConfigureAwait(false);
            }
        }
    }
}
