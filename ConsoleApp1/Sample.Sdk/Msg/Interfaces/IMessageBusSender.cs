using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Interfaces
{
    public interface IMessageBusSender
    {
        Task<bool> Send(string queueName, CancellationToken token, IEnumerable<ExternalMessage> messages, Action<IExternalMessage> onSent);
    }
}
