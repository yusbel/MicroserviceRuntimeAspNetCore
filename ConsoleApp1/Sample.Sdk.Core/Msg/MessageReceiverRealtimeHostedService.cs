using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Interface.Msg;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;

namespace Sample.Sdk.Core.Msg
{
    /// <summary>
    /// It will required specific task 
    /// </summary>
    public class MessageReceiverRealtimeHostedService : IHostedService
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly ILogger<MessageReceiverRealtimeHostedService> _logger;
        private readonly IMessageRealtimeService _messageRealtimeService;
        private Task? _innerTask;
        private Task? _outerTask;

        public MessageReceiverRealtimeHostedService(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<MessageReceiverRealtimeHostedService>>();
            var realtimeServices = serviceProvider.GetRequiredService<IEnumerable<IMessageRealtimeService>>();
            _messageRealtimeService = realtimeServices.Where(service => service is MessageReceiverService).FirstOrDefault()!;
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
                        await _messageRealtimeService.Compute(token).ConfigureAwait(false);
                    });
                    _innerTask.ConfigureAwait(false);
                    _innerTask.Wait(token);
                }
                catch (TaskCanceledException tce)
                {
                    tce.LogException(_logger.LogCritical);
                }
                catch (OperationCanceledException oe)
                {
                    oe.LogException(_logger.LogCritical);
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
                    _cancellationTokenSource.Cancel();
                }
                finally
                {
                    _cancellationTokenSource.Dispose();
                }
            }, token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();//TODO would it throw exceptions?
            }
            return Task.CompletedTask;
        }

    }
}
