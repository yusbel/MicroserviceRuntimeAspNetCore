using Sample.Sdk.InMemory;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging
{
    public class MemoryDispatcher : IMessageDispatcher
    {
        private readonly IInMemoryMessageBus<ExternalMessage> messageBus;

        public MemoryDispatcher(IInMemoryMessageBus<ExternalMessage> messageBus) 
        {
            this.messageBus = messageBus;
        }

        public Task<bool> Dispatch(string subscriberKey)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DispatchAll()
        {
            throw new NotImplementedException();
        }
    }
}
