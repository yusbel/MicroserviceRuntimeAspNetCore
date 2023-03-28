using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Services.Realtime.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Realtime
{
    /// <summary>
    /// Message sender hosted service
    /// </summary>
    public class MessageSenderRealtimeHostedService : IHostedService
    {
        private readonly IMessageRealtimeService _messageRealtimeService;
        private readonly ILogger<MessageSenderRealtimeHostedService> _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        public MessageSenderRealtimeHostedService(IEnumerable<IMessageRealtimeService> realtimeServices,
            ILogger<MessageSenderRealtimeHostedService> logger) 
        {
            _messageRealtimeService = realtimeServices
                                        .Where(service => service is MessageSenderService)
                                        .FirstOrDefault() 
                                        ?? throw new ArgumentNullException("Message sender realtime was not registered");
            _logger = logger;
        }
        /// <summary>
        /// Return hosted service task and launch message service compute task
        /// </summary>
        /// <param name="cancellationToken">Cancel hosted service operation</param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;
            var task = Task.Run(async () => 
            {
                try
                {
                    await _messageRealtimeService.Compute(token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
                }
            }, token);
            task.ConfigureAwait(false);
            _logger.LogInformation("Message send service was initiated");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stop message sender tasks 
        /// </summary>
        /// <param name="cancellationToken">Cancel token</param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
            }
            return Task.CompletedTask;
        }
    }
}
