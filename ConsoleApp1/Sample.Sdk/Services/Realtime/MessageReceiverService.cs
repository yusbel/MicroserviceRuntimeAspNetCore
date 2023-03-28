using Microsoft.Azure.Amqp.Framing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Core.Security.Interfaces;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.EntityModel;
using Sample.Sdk.InMemory;
using Sample.Sdk.InMemory.InMemoryListMessage;
using Sample.Sdk.Msg;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Data.Options;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services.Interfaces;
using Sample.Sdk.Services.Realtime.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Realtime
{
    public class MessageReceiverService : IMessageRealtimeService
    {
        private class ComputedMessage : IMessageIdentifier
        {
            public InComingEventEntity EventEntity { get; init; }
            public Expression<Func<InComingEventEntity, bool>> PropertyToUpdate { get; init; } = default;
            public string Id { get => EventEntity.Id; set => throw new NotImplementedException(); }
        }

        private static Lazy<IInMemoryDeDuplicateCache<InComingEventEntity>> inComingEventEntity = new(
            () => 
            {
                return new InMemoryDeDuplicateCache<InComingEventEntity>(
                    MemoryCacheState<string, string>.Instance(),
                    NullLoggerFactory.Instance.CreateLogger<InMemoryDeDuplicateCache<InComingEventEntity>>());
            }, true);
        private static Lazy<IInMemoryDeDuplicateCache<InCompatibleMessage>> inCompatibleMessage = new(
            () => 
            {
                return new InMemoryDeDuplicateCache<InCompatibleMessage>(
                    MemoryCacheState<string, string>.Instance(),
                    NullLoggerFactory.Instance.CreateLogger<InMemoryDeDuplicateCache<InCompatibleMessage>>());
            }, true);
        private static Lazy<IInMemoryDeDuplicateCache<CorruptedMessage>> corruptedMessages = new(
            () => 
            {
                return new InMemoryDeDuplicateCache<CorruptedMessage>(
                    MemoryCacheState<string,string>.Instance(),
                    NullLoggerFactory.Instance.CreateLogger<InMemoryDeDuplicateCache<CorruptedMessage>>());
            }, true);
        private static Lazy<IInMemoryDeDuplicateCache<InComingEventEntity>> ackMessages = new(
            () => 
            {
                return new InMemoryDeDuplicateCache<InComingEventEntity>(
                    MemoryCacheState<string,string>.Instance(),
                    NullLoggerFactory.Instance.CreateLogger<InMemoryDeDuplicateCache<InComingEventEntity>>());
            }, true);
        private static Lazy<IInMemoryDeDuplicateCache<ComputedMessage>> persistMessages = new(
            () => 
            {
                return new InMemoryDeDuplicateCache<ComputedMessage>(
                    MemoryCacheState<string,string>.Instance(),
                    NullLoggerFactory.Instance.CreateLogger<InMemoryDeDuplicateCache<ComputedMessage>>());
            }, true);

        private readonly IMessageComputation _computations;
        private readonly IComputeExternalMessage _computeExternalMessage;
        private readonly IInMemoryDeDuplicateCache<InComingEventEntity> _inComingEvents = inComingEventEntity.Value;
        private readonly IInMemoryDeDuplicateCache<InCompatibleMessage> _incompatibleMessages = inCompatibleMessage.Value;
        private readonly IInMemoryDeDuplicateCache<CorruptedMessage> _corruptedMessages = corruptedMessages.Value;
        private readonly IInMemoryDeDuplicateCache<InComingEventEntity> _ackMessages = ackMessages.Value;
        private readonly IInMemoryDeDuplicateCache<ComputedMessage> _persistMessages = persistMessages.Value;

        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<List<AzureMessageSettingsOptions>> _messagingOptions;
        private readonly IMessageSender _messageSender;
        private readonly IMessageReceiver _messageBusReceiver;
        private readonly ILogger<MessageReceiverService> _logger;
        private readonly IMessageCryptoService _cryptoService;
        private CancellationTokenSource? _cancellationTokenSource;
        public MessageReceiverService(
            IMessageComputation computations,
            IComputeExternalMessage computeExternalMessage,
            IMessageReceiver messageBusReceiver,
            ILogger<MessageReceiverService> logger,
            IMessageCryptoService cryptoService,
            IAsymetricCryptoProvider asymetricCryptoProvider,
            IServiceProvider serviceProvider,
            IOptions<List<AzureMessageSettingsOptions>> messagingOptions, 
            IMessageSender messageSender)
        {
            _computations = computations;
            _computeExternalMessage = computeExternalMessage;
            _messageBusReceiver = messageBusReceiver;
            _logger = logger;
            _cryptoService = cryptoService;
            _asymetricCryptoProvider = asymetricCryptoProvider;
            _serviceProvider = serviceProvider;
            _messagingOptions = messagingOptions;
            _messageSender = messageSender;
        }

        public async Task Compute(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;
            var tasks = new List<Task>();
            var retrievalTask = ReceiveMessages(token);
            _ = retrievalTask.ConfigureAwait(false);
            tasks.Add(retrievalTask);
            var taskFromDb = RetrieveInComingEventEntityFromDatabase(token);
            _ = taskFromDb.ConfigureAwait(false);
            tasks.Add(taskFromDb);
            var taskCompute = ComputeReceivedMessage(token);
            _ = taskCompute.ConfigureAwait(false);
            tasks.Add(taskCompute);
            var taskUpdate = UpdateEventStatus(token);
            _ = taskUpdate.ConfigureAwait(false);
            tasks.Add(taskUpdate);
            var taskRetrieveAck = RetrieveAcknowledgementMessage(token);
            _ = taskRetrieveAck.ConfigureAwait(false);
            tasks.Add(taskRetrieveAck);
            var taskSentAck = SendAckMessages(token);
            _ = taskSentAck.ConfigureAwait(false);
            //tasks.Add(taskSentAck);
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
                        var msgToSend = messages.ToList().ConvertAll(msg => { return msg.ConvertToExternalMessage(); });

                        await _messageSender.SendMessages((msg)=> msg.AckQueueName, msgToSend, msgs =>
                        {
                            msgs.ToList().ForEach(msg => 
                            {
                                var computedMessage = new ComputedMessage() 
                                { 
                                    EventEntity = msg.ConvertToInComingEventEntity(), 
                                    PropertyToUpdate = (message) => message.WasAcknowledge 
                                };
                                computedMessage.EventEntity.WasAcknowledge = true;
                                _persistMessages.TryAdd(computedMessage);
                            });

                        }, (msgs, exception) => 
                        {   
                        }, token).ConfigureAwait(false);

                        await Task.Delay(1000, token).ConfigureAwait(false);
                    }
                    await Task.Delay(1000, token).ConfigureAwait(false);
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
                                                    token).ConfigureAwait(false);
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

        private async Task UpdateEventStatus(CancellationToken cancellationToken)
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
                                await _computations.UpdateEventStatus(scope, 
                                                            computedMessage.EventEntity,
                                                            computedMessage.PropertyToUpdate,
                                                            token)
                                    .ConfigureAwait(false);
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
                                await Task.Delay(1000, token).ConfigureAwait(false);
                            }
                        }
                        await Task.Delay(TimeSpan.FromMinutes(1), token).ConfigureAwait(false);
                    }
                    await Task.Delay(TimeSpan.FromMinutes(1), token).ConfigureAwait(false);
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

        private async Task ComputeReceivedMessage(CancellationToken cancellationToken)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = cancellationTokenSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    while (_inComingEvents.TryTakeAllWithoutDuplicate(out var messages, token))
                    {
                        token.ThrowIfCancellationRequested();
                        //await Parallel.ForEachAsync(messages, token, async (message, token) =>
                        foreach(var message in messages)
                        {
                            EncryptedMessage? encryptedMessage = null;
                            try
                            {
                                encryptedMessage = JsonSerializer.Deserialize<EncryptedMessage>(message.Body);
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

                            (bool wasEncrypted, List<KeyValuePair<string,string>> externalMsg, EncryptionDecryptionFail reason) decryptorResult;
                            try
                            {
                                decryptorResult = await _cryptoService.GetDecryptedExternalMessage(encryptedMessage!,
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
                                    await _computeExternalMessage.ProcessExternalMessage(decryptorResult.externalMsg!, token).ConfigureAwait(false);
                                    message.WasProcessed = true;
                                    var computedMessage = new ComputedMessage() { EventEntity = message, PropertyToUpdate = (msg) => msg.WasProcessed };
                                    _persistMessages.TryAdd(computedMessage);
                                    _ackMessages.TryAdd(message);
                                }
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception e)
                            {
                                e.LogException(_logger.LogCritical);
                            }
                        };//);
                        await Task.Delay(1000, token).ConfigureAwait(false);
                    }
                    await Task.Delay(1000, token).ConfigureAwait(false);
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
                                    token).ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
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
        private Task ReceiveMessages(CancellationToken cancellationToken)
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
                            var settings = _messagingOptions.Value.Where(item => item.ConfigType == 
                                                                    Core.Enums.Enums.AzureMessageSettingsOptionType.Receiver)
                                                            .ToList();
                            foreach (var msgSettings in settings) 
                            {
                                var tasks = new List<Task>();
                                foreach (var msgInTransitOptions in msgSettings.MessageInTransitOptions) 
                                {
                                    var task = Task.Run(async () => 
                                    {
                                        using var scope = _serviceProvider.CreateScope();
                                        await _messageBusReceiver.Receive(
                                                            token,
                                                            async (inComingEvent, token) =>
                                                            {
                                                                await _computations.SaveInComingEventEntity(scope, inComingEvent, token).ConfigureAwait(false);
                                                                _inComingEvents.TryAdd(inComingEvent);//realtime message
                                                                return true;
                                                            }, msgInTransitOptions.MsgQueueName.ToLower())
                                                    .ConfigureAwait(false);
                                    }, token);
                                    task.ConfigureAwait(false);
                                    tasks.Add(task);
                                }
                                await Task.WhenAll(tasks);
                            }
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
