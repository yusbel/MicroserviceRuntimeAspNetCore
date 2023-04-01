using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Services.Interfaces;
using Sample.Sdk.Core;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;

namespace Sample.EmployeeSubdomain.Services
{
    /// <summary>
    /// Hosted service to send external messages to azure messaging
    /// </summary>
    public class MessageSenderHostedService : IHostedService
    {
        private readonly IMessageSenderService _messageSenderService;
        private readonly ILogger<MessageSenderHostedService> _logger;
        private CancellationTokenSource? _cancellationTokenSource;
        public MessageSenderHostedService(IMessageSenderService messageSenderService
                                        , ILogger<MessageSenderHostedService> logger)
        {
            Guard.ThrowWhenNull(messageSenderService, logger);
            _messageSenderService = messageSenderService;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;
            var task = Task.Run(async () => 
            {
                try
                {
                    await _messageSenderService.Send(token).ConfigureAwait(false);
                }
                catch (Exception e) 
                {
                    e.LogException(_logger.LogCritical, "Message sender service hosted is stopping.");
                    _cancellationTokenSource.Cancel();
                }
            }, token);
            task.ConfigureAwait(false);
            return task;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            return Task.CompletedTask;
        }
    }
}
