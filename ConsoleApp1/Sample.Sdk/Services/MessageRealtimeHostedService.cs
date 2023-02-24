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
    public class MessageRealtimeHostedService<T> : IHostedService, IDisposable where T : class, IMessageIdentifier
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly ILogger<MessageRealtimeHostedService<T>> _logger;
        private readonly IHostApplicationLifetime _hostAppLifetime;
        private Task? _innerTask;
        private Task? _outerTask;

        public MessageRealtimeHostedService(
            ILogger<MessageRealtimeHostedService<T>> logger,
            IHostApplicationLifetime hostAppLifetime)
        {
            _logger = logger;
            _hostAppLifetime = hostAppLifetime;
        }

        private void InitializeHostApplicationLifetimeCancellation()
        {
            _hostAppLifetime.ApplicationStopping.Register(() =>
            {
                try
                {
                    var token = _cancellationTokenSource?.Token;
                    if (token.HasValue && !token.Value.IsCancellationRequested)
                    {
                        _cancellationTokenSource?.Cancel();
                    }
                }
                catch (OperationCanceledException oe)
                {
                    oe.LogException(_logger, "An operation canceled exception was raised when stopping the service");
                }
                catch (Exception e)
                {
                    e.LogException(_logger, "An error ocurred when cancelling the task");
                }
            });
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
            InitializeHostApplicationLifetimeCancellation();

            _outerTask = Task.Run(() =>
            {
                try
                {
                    _innerTask = Task.Run(() => 
                    {

                    });
                    _innerTask.Wait(token);
                }
                catch (TaskCanceledException tce) 
                {
                    tce.LogException(_logger, "Task was cancelled");
                }
                catch (OperationCanceledException oe)
                {
                    oe.LogException(_logger, "An error ocurred");
                }
                catch (Exception e)
                {
                    e.LogException(_logger, "An error ocurred");
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
                e.LogException(_logger, "Errors ocurred when stopping the service", nameof(MessageRealtimeHostedService<T>));
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_cancellationTokenSource != null
                            && _cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Dispose();
            }
        }
    }
}
