using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Interfaces;
using Sample.EmployeeSubdomain.Services.Interfaces;
using Sample.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Services
{
    public class MessageSenderHostedService : BackgroundService
    {
        private readonly IMessageSenderService _messageSenderService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MessageSenderHostedService> _logger;

        public MessageSenderHostedService(IMessageSenderService messageSenderService
                                        , IServiceScopeFactory serviceScopeFactory
                                        , ILoggerFactory loggerFactory)
        {
            Guard.ThrowWhenNull(messageSenderService, serviceScopeFactory);
            _messageSenderService = messageSenderService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = loggerFactory.CreateLogger<MessageSenderHostedService>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var excecutingTask = _messageSenderService.Send(stoppingToken);
                if(excecutingTask.IsCompleted) 
                {
                    return excecutingTask;
                }
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                _logger.LogCritical("Message:{} StackTrace: {}", e.Message, e.StackTrace);
            }
            return Task.CompletedTask;
        }

    }
}
