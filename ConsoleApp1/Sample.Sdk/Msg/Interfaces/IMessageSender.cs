using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Interfaces
{
    public interface IMessageSender
    {
        Task<int> Send(CancellationToken token
                        , IEnumerable<ExternalMessage> messages
                        , Action<ExternalMessage> onSent);
    }
}
