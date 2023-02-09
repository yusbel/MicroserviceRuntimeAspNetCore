using Microsoft.Extensions.Logging;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging
{
    public class LoggerDispatcher : IMessageDispatcher
    {
        private ILogger<LoggerDispatcher> _logger;

        public LoggerDispatcher(ILoggerFactory loggerFactory) => _logger = loggerFactory.CreateLogger<LoggerDispatcher>();
        public Task<bool> Dispatch(string key, IExternalMessage message)
        {
            _logger.LogInformation($"Logger dispather message {message.GetType().FullName}");
            return Task.FromResult(true);
        }

        public Task<bool> Dispatch(string key, string message)
        {
            _logger.LogInformation($"Logger dispatcher message string {message}");
            return Task.FromResult(true);
        }
    }
}
