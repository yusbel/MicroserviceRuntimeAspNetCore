using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging
{
    public class MemmoryDispatcher : IMessageDispatcher
    {
        public Task<bool> Dispatch(string key, IExternalMessage message)
        {
            InMemmoryMessage<IExternalMessage>.Create().Add(key, message);
            return Task.FromResult(true);
        }

        public Task<bool> Dispatch(string key, string message)
        {
            InMemmoryMessage<string>.Create().Add(key, message);
            return Task.FromResult(true);
        }
    }
}
