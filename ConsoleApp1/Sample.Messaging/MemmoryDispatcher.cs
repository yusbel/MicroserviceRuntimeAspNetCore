using Sample.Sdk.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging
{
    public class MemmoryDispatcher : IMessageDispatcher
    {
        public Task<bool> Dispatch(string key, IMessage message)
        {
            InMemmoryMessage<IMessage>.Create().Add(key, message);
            return Task.FromResult(true);
        }

        public Task<bool> Dispatch(string key, string message)
        {
            InMemmoryMessage<string>.Create().Add(key, message);
            return Task.FromResult(true);
        }
    }
}
