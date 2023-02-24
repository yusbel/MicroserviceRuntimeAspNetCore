using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services
{
    public class MessageRealtimeService<T> where T : class, IMessageIdentifier
    {
        private readonly IMessageComputation<T> _computations;
        private readonly IInMemoryProducerConsumerCollection<InComingEventEntity> _inComingEvents;
        private readonly IMessageBusReceiver<ExternalMessage> _messageBusReceiver;
        private readonly ILogger<MessageRealtimeService<T>> _logger;
        private CancellationTokenSource _cancellationTokenSource;
        public MessageRealtimeService(
            IMessageComputation<T> computations,
            IInMemoryProducerConsumerCollection<InComingEventEntity> inComingEvents,
            IMessageBusReceiver<ExternalMessage> messageBusReceiver,
            ILogger<MessageRealtimeService<T>> logger)
        {
            _computations = computations;
            _inComingEvents = inComingEvents;
            _messageBusReceiver = messageBusReceiver;
            _logger = logger;
        }

        public async Task Compute(CancellationToken cancellationToken) 
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;
            var tasks = new List<Task>();
            tasks.Add(RunRealtimeMessageRetrieval(token));
            tasks.Add(RetrieveInComingEventEntityFromDatabase(token));

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

        private async Task ComputeInRealtime(CancellationToken cancellationToken) 
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = cancellationTokenSource.Token;

            while(!token.IsCancellationRequested) 
            {
                while (_inComingEvents.TryTakeAllWithoutDuplicate(out var messages, token)) 
                {
                    await Parallel.ForEachAsync(messages, (message, token) => 
                    {
                        
                        return ValueTask.CompletedTask;
                    });
                    await Task.Delay(1000);
                }
                await Task.Delay(1000);
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
                                    (eventEntity) => !eventEntity.IsDeleted
                                                    && !eventEntity.WasAcknowledge
                                                    && !eventEntity.WasProcessed,
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
