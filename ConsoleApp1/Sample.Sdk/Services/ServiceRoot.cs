using Microsoft.Extensions.Options;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services
{
    public abstract class ServiceRoot : IServiceMessageReceiver
    {
        private readonly IOptions<List<ServiceBusInfoOptions>> _options;

        public ServiceRoot(IOptions<List<ServiceBusInfoOptions>> options)
        {
            
            _options = options;
        }

        protected IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetAvailableQueues()
        {
            if (_options == null || _options.Value == null || _options.Value.Count == 0)
            {
                return Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
            }
            return _options.Value
                    .Select(kvp => KeyValuePair.Create(kvp.Identifier, kvp.QueueNames.Split(',').AsEnumerable()))
                    .AsEnumerable();
        }
        protected abstract IEnumerable<Func<Task<ExternalMessage>>> GetMessageReceivers(CancellationToken token);

        public async Task Process(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                
            }
        }
    }
}
