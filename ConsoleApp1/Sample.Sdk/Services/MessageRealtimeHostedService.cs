using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services
{
    /// <summary>
    /// It will required specific task 
    /// </summary>
    public class MessageRealtimeHostedService<T> : IHostedService where T : class, IMessageIdentifier
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly ILogger<MessageRealtimeHostedService<T>> _logger;
        private readonly IMessageRealtimeService _messageRealtimeService;
        private Task? _innerTask;
        private Task? _outerTask;

        public MessageRealtimeHostedService(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<MessageRealtimeHostedService<T>>>();
            _messageRealtimeService = serviceProvider.GetRequiredService<IMessageRealtimeService>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            _outerTask = Task.Run(() =>
            {
                try
                {
                    _innerTask = Task.Run(async () => 
                    {
                        await _messageRealtimeService.Compute(token);
                    });
                    _innerTask.Wait(token);
                }
                catch (TaskCanceledException tce) 
                {
                    tce.LogCriticalException(_logger, "Task was cancelled");
                }
                catch (OperationCanceledException oe)
                {
                    oe.LogCriticalException(_logger, "An error ocurred");
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "An error ocurred");
                    _cancellationTokenSource.Cancel();
                }
                finally 
                {
                    _cancellationTokenSource.Dispose();
                }
            }, token);
            return _outerTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "Errors ocurred when stopping the service", nameof(MessageRealtimeHostedService<T>));
            }
            finally 
            {
                _cancellationTokenSource?.Dispose();//TODO would i throw exceptions
            }
            return Task.CompletedTask;
        }

    }
}
