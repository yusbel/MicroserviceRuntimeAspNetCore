using Microsoft.Extensions.Logging;
using Sample.Sdk;
using Sample.Sdk.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging
{
    public class Dispatcher
    {
        private readonly ILogger<Dispatcher> _logger;
        private readonly IEnumerable<IMessageDispatcher> _messageDispatcher;
        public Dispatcher(ILoggerFactory loggerFactory, IEnumerable<IMessageDispatcher> dispatchers) => (_logger, _messageDispatcher) = (loggerFactory.CreateLogger<Dispatcher>(), dispatchers); 
        public Task Dispath(string path, string key, string msg) 
        {
            Guard.ThrowWhenNull(key, msg);
            //_messageDispatcher.ToList().ForEach(dispatcher => dispatcher.Dispatch(key, msg));
            return Task.CompletedTask;
        }
    }
}
