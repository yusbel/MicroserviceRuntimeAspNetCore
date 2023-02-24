using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.InMemoryListMessage;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample.Sdk.Services
{
    public class MessageRealtimeService<T> where T : class, IMessageIdentifier
    {
        private readonly IMessageComputation<T> _computations;
        private readonly IInMemoryProducerConsumerCollection<InComingEventEntityInMemoryList, InComingEventEntity> _inComingEvents;
        private readonly IInMemoryProducerConsumerCollection<InCompatibleMessageInMemoryList, InCompatibleMessage> _incompatibleMessages;
        private readonly IInMemoryProducerConsumerCollection<CorruptedMessageInMemoryList, CorruptedMessage> _corruptedMessages;
        private readonly IInMemoryProducerConsumerCollection<AcknowledgementMessageInMemoryList, InComingEventEntity> _ackMessages;
        private readonly IInMemoryProducerConsumerCollection<ComputedMessageInMemoryList, InComingEventEntity> _computedMessages;
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;
        private readonly IMessageBusReceiver<ExternalMessage> _messageBusReceiver;
        private readonly ILogger<MessageRealtimeService<T>> _logger;
        private readonly IDecryptorService _decryptorService;
        private readonly IAcknowledgementService _acknowledgementService;
        private CancellationTokenSource? _cancellationTokenSource;
        public MessageRealtimeService(
            IMessageComputation<T> computations,
            IMessageBusReceiver<ExternalMessage> messageBusReceiver,
            ILogger<MessageRealtimeService<T>> logger,
            IDecryptorService decryptorService,
            IAcknowledgementService acknowledgementService,
            IInMemoryProducerConsumerCollection<ComputedMessageInMemoryList, InComingEventEntity> computedMessages,
            IInMemoryProducerConsumerCollection<InComingEventEntityInMemoryList, InComingEventEntity> inComingEvents,
            IInMemoryProducerConsumerCollection<InCompatibleMessageInMemoryList, InCompatibleMessage> incompatibleMessages,
            IInMemoryProducerConsumerCollection<CorruptedMessageInMemoryList, CorruptedMessage> corruptedMessages,
            IInMemoryProducerConsumerCollection<AcknowledgementMessageInMemoryList, InComingEventEntity> ackMessages,
            IAsymetricCryptoProvider asymetricCryptoProvider)
        {
            _computations = computations;
            _inComingEvents = inComingEvents;
            _messageBusReceiver = messageBusReceiver;
            _logger = logger;
            _decryptorService = decryptorService;
            _acknowledgementService = acknowledgementService;
            _computedMessages = computedMessages;
            _incompatibleMessages = incompatibleMessages;
            _corruptedMessages = corruptedMessages;
            _ackMessages = ackMessages;
            _asymetricCryptoProvider = asymetricCryptoProvider;
        }

        public async Task Compute(CancellationToken cancellationToken) 
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;
            var tasks = new List<Task>();
            tasks.Add(RunRealtimeMessageRetrieval(token));
            tasks.Add(RetrieveInComingEventEntityFromDatabase(token));
            tasks.Add(ComputeInRealtime(token));
            tasks.Add(UpdateInComingEventEntity(token));
            tasks.Add(RetrieveAcknowledgementMessage(token));
            tasks.Add(SendAckMessages(token));
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                e.LogException(_logger, "An error ocurred when waiting for all task to complete");
            }
            finally 
            {
                _cancellationTokenSource.Dispose();
            }
        }

        private async Task SendAckMessages(CancellationToken cancellationToken) 
        {
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = tokenSource.Token;
            try
            {
                while(!token.IsCancellationRequested) 
                {
                    while (_ackMessages.TryTakeAllWithoutDuplicate(out var messages, token)) 
                    {
                        //await _acknowledgementService.SendAcknowledgement()
                        await Task.Delay(1000);
                    }
                    await Task.Delay(1000);
                }

            }
            catch (Exception e)
            {
                e.LogException(_logger, "An error ocurred when sending ack messages");
            }
            finally 
            {

            }
        }

        private async Task RetrieveAcknowledgementMessage(CancellationToken cancellationToken) 
        {
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = tokenSource.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var messages = await _computations.GetInComingEventsAsync(
                                                    (incomingEvent) => !incomingEvent.IsDeleted &&
                                                                        !incomingEvent.WasAcknowledge &&
                                                                        incomingEvent.WasProcessed,
                                                    token);
                    if (messages != null)
                    {
                        foreach (var msg in messages)
                        {
                            _ackMessages.TryAdd(msg);
                        }
                    }
                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            }
            catch (Exception e) 
            {
                e.LogException(_logger, "An error ocurred when retriving message from incoming event entity table to send acknowldegement");
            }
            finally
            {
                tokenSource.Dispose();
            }
        }

        private async Task UpdateInComingEventEntity(CancellationToken cancellationToken) 
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = cancellationTokenSource.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    while (_computedMessages.TryTakeAllWithoutDuplicate(out var computedMessages, token))
                    {
                        foreach (var computedMessage in computedMessages)
                        {
                            await _computations.UpdateInComingEventEntity(computedMessage, token);
                        }
                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
            catch (Exception e) 
            {
                e.LogException(_logger, "Error ocurred when updating message");
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }

        private async Task ComputeInRealtime(CancellationToken cancellationToken) 
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = cancellationTokenSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    while (_inComingEvents.TryTakeAllWithoutDuplicate(out var messages, token))
                    {
                        await Parallel.ForEachAsync(messages, async (message, token) =>
                        {
                            EncryptedMessageMetadata? encryptedMessageWithMetadata = null;
                            try
                            {
                                encryptedMessageWithMetadata = JsonSerializer.Deserialize<EncryptedMessageMetadata>(message.Body);
                            }
                            catch (Exception e)
                            {
                                e.LogException(_logger, $"Unable to deserialize the message with message key {message.MessageKey} and message id {message.Id}");
                                _incompatibleMessages.TryAdd(new InCompatibleMessage()
                                {
                                    OriginalMessageKey = message.MessageKey,
                                    Id = message.Id,
                                    EncryptedContent = message.Body,
                                    OriginalType = message.Type,
                                    InCompatibleType = nameof(EncryptedMessageMetadata)
                                });
                                return;
                            }

                            (bool wasEncrypted, ExternalMessage? externalMsg, EncryptionDecryptionFail reason) decryptorResult;
                            try
                            {
                                decryptorResult = await _decryptorService.GetDecryptedExternalMessage(encryptedMessageWithMetadata!,
                                                                                    _asymetricCryptoProvider,
                                                                                    cancellationToken);
                            }
                            catch (Exception e)
                            {
                                e.LogException(_logger, $"An error ocurred when decrypting message with key {message.MessageKey} and identifier {message.Id}");
                                return;
                            }
                            try
                            {
                                if (decryptorResult.wasEncrypted)
                                {
                                    await _computations.ProcessExternalMessage(decryptorResult.externalMsg!, token);
                                    _computedMessages.TryAdd(message);
                                }
                            }
                            catch (Exception e)
                            {
                                e.LogException(_logger, $"An error ocurred when processing the message with key {message.MessageKey} and identifier {message.Id}");
                            }
                        });
                        await Task.Delay(1000);
                    }
                    await Task.Delay(1000);
                }
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// It will query the table on a delayed time as event entity are added into the realtime computation as they arrive.
        /// It serve as gurantee to not miss event.
        /// It might produce duplicate, the message realtime de duplicate message as they are being retrieved.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private Task RetrieveInComingEventEntityFromDatabase(CancellationToken cancellationToken) 
        {
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = tokenSource.Token;
            return Task.Run(async() => 
            {
                try 
                {
                    while (!token.IsCancellationRequested)
                    {
                        await _computations.GetInComingEventsAsync(
                                    (eventEntity) =>    !eventEntity.IsDeleted &&
                                                        !eventEntity.WasAcknowledge &&
                                                        !eventEntity.WasProcessed,
                                    token); 
                        await Task.Delay(TimeSpan.FromMinutes(5));
                    }

                }
                catch (Exception e) 
                {
                    e.LogException(_logger, "An error ocurred");
                }
                finally 
                { 
                    tokenSource.Dispose(); 
                }
            }, token);
        }

        /// <summary>
        /// Retrieve message from azure service bus and save them in the database table incoming event entity, 
        /// also add the event into the realtime computation
        /// Use token cancellation source to stop down the stream tasks
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task RunRealtimeMessageRetrieval(CancellationToken cancellationToken) 
        {
            CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = tokenSource.Token;
            return Task.Run(async() => 
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            await _messageBusReceiver.Receive(token,
                                                async (inComingEvent, token) =>
                                                {
                                                    await _computations.SaveInComingEventEntity(inComingEvent, token);
                                                    _inComingEvents.TryAdd(inComingEvent);//realtime message
                                                    return true;
                                                });
                        }
                        catch (Exception e)
                        {
                            e.LogException(_logger, "");
                            //TODO:Inspect exception from database to slow the loop count and raise cancel operation.
                        }
                    }
                }
                catch (Exception e)
                {
                    e.LogException(_logger, "An error ocurred");
                }
                finally 
                {
                    tokenSource.Dispose();
                }
            }, token);
        }
    }
}
