using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Interfaces
{
    public interface IMessageBusReceiver<T>
    {
        public Task<T> Receive(CancellationToken token, Func<ExternalMessage, Task<ExternalMessage>> processBeforeCompleted, string queueName = "");
    }
}   
