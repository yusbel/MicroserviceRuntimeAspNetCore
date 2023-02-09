using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Interfaces
{
    public interface IMessageBusReceiver<T> where T : ExternalMessage
    {
        public Task<T> Receive(CancellationToken token, Func<T,Task<T>> processBeforeCompleted, string queueName = "");
    }
}   
