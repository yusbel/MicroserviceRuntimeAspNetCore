using Microsoft.Azure.Amqp.Framing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Interfaces;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.EntityModel;
using Sample.Sdk.InMemory.InMemoryListMessage;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services.Interfaces;
using Sample.Sdk.Services.Realtime.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Realtime
{
    public class MessageReceiverRealtimeService<T> : IMessageRealtimeService where T : class, IMessageIdentifier
    {
        private readonly IMessageComputation<T> _computations;
        private readonly IInMemoryDeDuplicateCache<InComingEventEntityInMemoryList, InComingEventEntity> _inComingEvents;
        private readonly IInMemoryDeDuplicateCache<InCompatibleMessageInMemoryList, InCompatibleMessage> _incompatibleMessages;
        private readonly IInMemoryDeDuplicateCache<CorruptedMessageInMemoryList, CorruptedMessage> _corruptedMessages;
        private readonly IInMemoryDeDuplicateCache<AcknowledgementMessageInMemoryList, InComingEventEntity> _ackMessages;
        private readonly IInMemoryDeDuplicateCache<ComputedMessageInMemoryList, InComingEventEntity> _persistMessages;
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessageBusReceiver<ExternalMessage> _messageBusReceiver;
        private readonly ILogger<MessageReceiverRealtimeService<T>> _logger;
        private readonly IMessageCryptoService _cryptoService;
        private readonly IAcknowledgementService _acknowledgementService;
        private CancellationTokenSource? _cancellationTokenSource;
        public MessageReceiverRealtimeService(
            IMessageComputation<T> computations,
            IMessageBusReceiver<ExternalMessage> messageBusReceiver,
            ILogger<MessageReceiverRealtimeService<T>> logger,
            IMessageCryptoService cryptoService,
            IAcknowledgementService acknowledgementService,
            IInMemoryDeDuplicateCache<ComputedMessageInMemoryList, InComingEventEntity> persistMessages,
            IInMemoryDeDuplicateCache<InComingEventEntityInMemoryList, InComingEventEntity> inComingEvents,
            IInMemoryDeDuplicateCache<InCompatibleMessageInMemoryList, InCompatibleMessage> incompatibleMessages,
            IInMemoryDeDuplicateCache<CorruptedMessageInMemoryList, CorruptedMessage> corruptedMessages,
            IInMemoryDeDuplicateCache<AcknowledgementMessageInMemoryList, InComingEventEntity> ackMessages,
            IAsymetricCryptoProvider asymetricCryptoProvider,
            IServiceProvider serviceProvider)
        {
            _computations = computations;
            _inComingEvents = inComingEvents;
            _messageBusReceiver = messageBusReceiver;
            _logger = logger;
            _cryptoService = cryptoService;
            _acknowledgementService = acknowledgementService;
            _persistMessages = persistMessages;
            _incompatibleMessages = incompatibleMessages;
            _corruptedMessages = corruptedMessages;
            _ackMessages = ackMessages;
            _asymetricCryptoProvider = asymetricCryptoProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task Compute(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;
            var tasks = new List<Task>();
            var retrievalTask = RunRealtimeMessageRetrieval(token);
            _ = retrievalTask.ConfigureAwait(false);
            tasks.Add(retrievalTask);
            var taskFromDb = RetrieveInComingEventEntityFromDatabase(token);
            _ = taskFromDb.ConfigureAwait(false);
            tasks.Add(taskFromDb);
            var taskCompute = ComputeInRealtime(token);
            _ = taskCompute.ConfigureAwait(false);
            tasks.Add(taskCompute);
            var taskUpdate = UpdateInComingEventEntity(token);
            _ = taskUpdate.ConfigureAwait(false);
            tasks.Add(taskUpdate);
            var taskRetrieveAck = RetrieveAcknowledgementMessage(token);
            _ = taskRetrieveAck.ConfigureAwait(false);
            tasks.Add(taskRetrieveAck);
            var taskSentAck = SendAckMessages(token);
            _ = taskSentAck.ConfigureAwait(false);
            tasks.Add(taskSentAck);
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce)
            {
                oce.LogException(_logger.LogCritical);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                _cancellationTokenSource?.Cancel();
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        private async Task SendAckMessages(CancellationToken cancellationToken)
        {
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = tokenSource.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    while (_ackMessages.TryTakeAllWithoutDuplicate(out var messages, token))
                    {
                        foreach (var message in messages)
                        {
                            EncryptedMessage encryptMsgMetadata;
                            try
                            {
                                encryptMsgMetadata = JsonSerializer.Deserialize<EncryptedMessage>(message.Body);
                            }
                            catch (Exception e)
                            {
                                _logger.LogCritical(e, "An error ocurred when deserializing message from database");
                                continue;
                            }
                            if (encryptMsgMetadata == null)
                            {
                                _logger.LogCritical($"A message in the database incomming events can not be deserialized to encrypted message metadata");
                                continue;
                            }
                            (bool wasSent, EncryptionDecryptionFail reason) sentResult;
                            try
                            {
                                sentResult = await _acknowledgementService.SendAcknowledgement(message.Body, encryptMsgMetadata, token);
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception e)
                            {
                                _logger.LogCritical(e, "An error ocurred when sending the acknowledge message to sender");
                                await Task.Delay(1000); //adding delay in case is a glitch
                                continue;
                            }
                            if (sentResult.wasSent)
                            {
                                message.WasAcknowledge = true;
                                _persistMessages.TryAdd(message);
                            }
                        }

                        await Task.Delay(1000);
                    }
                    await Task.Delay(1000);
                }

            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
            }
            finally
            {
                tokenSource.Dispose();
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
                    using var scope = _serviceProvider.CreateScope();
                    var messages = await _computations.GetInComingEventsAsync(
                                                    scope,
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
                e.LogException(_logger.LogCritical);
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
                    while (_persistMessages.TryTakeAllWithoutDuplicate(out var computedMessages, token))
                    {
                        foreach (var computedMessage in computedMessages)
                        {
                            try
                            {
                                using var scope = _serviceProvider.CreateScope();
                                await _computations.UpdateInComingEventEntity(scope, computedMessage, token);
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (DbUpdateException) { throw; }
                            catch (DbEntityValidationException) { throw; }
                            catch (NotSupportedException) { throw; }
                            catch (ObjectDisposedException) { throw; }
                            catch (InvalidOperationException) { throw; }
                            catch (Exception e)
                            {
                                e.LogException(_logger.LogCritical);
                                await Task.Delay(1000);
                            }
                        }
                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
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
                        await Parallel.ForEachAsync(messages, token, async (message, token) =>
                        {
                            EncryptedMessage? encryptedMessageWithMetadata = null;
                            try
                            {
                                encryptedMessageWithMetadata = JsonSerializer.Deserialize<EncryptedMessage>(message.Body);
                            }
                            catch (Exception e)
                            {
                                e.LogException(_logger.LogCritical);
                                _incompatibleMessages.TryAdd(new InCompatibleMessage()
                                {
                                    OriginalMessageKey = message.MessageKey,
                                    Id = message.Id,
                                    EncryptedContent = message.Body,
                                    OriginalType = message.Type,
                                    InCompatibleType = nameof(EncryptedMessage)
                                });
                                return;
                            }

                            (bool wasEncrypted, Dictionary<string,string> externalMsg, EncryptionDecryptionFail reason) decryptorResult;
                            try
                            {
                                decryptorResult = await _cryptoService.GetDecryptedExternalMessage(encryptedMessageWithMetadata!,
                                                                                                    cancellationToken)
                                                                        .ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception e)
                            {
                                e.LogException(_logger.LogCritical);
                                return;
                            }
                            try
                            {
                                if (decryptorResult.wasEncrypted)
                                {
                                    using var scope = _serviceProvider.CreateScope();
                                    await _computations.ProcessExternalMessage(scope, decryptorResult.externalMsg!, token);
                                    message.WasProcessed = true;
                                    _persistMessages.TryAdd(message);
                                }
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception e)
                            {
                                e.LogException(_logger.LogCritical);
                            }
                        });
                        await Task.Delay(1000);
                    }
                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
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
            return Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        await _computations.GetInComingEventsAsync(
                                    scope,
                                    (eventEntity) => !eventEntity.IsDeleted &&
                                                        !eventEntity.WasAcknowledge &&
                                                        !eventEntity.WasProcessed,
                                    token);
                        await Task.Delay(TimeSpan.FromMinutes(5));
                    }

                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
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
            return Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            await _messageBusReceiver.Receive(
                                                token,
                                                async (inComingEvent, token) =>
                                                {
                                                    await _computations.SaveInComingEventEntity(scope, inComingEvent, token);
                                                    _inComingEvents.TryAdd(inComingEvent);//realtime message
                                                    return true;
                                                });
                        }
                        catch (Exception e)
                        {
                            e.LogException(_logger.LogCritical);
                            //TODO:Inspect exception from database to slow the loop count and raise cancel operation.
                        }
                    }
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
                }
                finally
                {
                    tokenSource.Dispose();
                }
            }, token);
        }
    }
}
