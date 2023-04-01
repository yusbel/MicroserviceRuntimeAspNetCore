using Sample.Sdk.Data.Entities;
using Sample.Sdk.Data.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Interface.Msg
{
    public interface IMessageReceiver
    {
        public Task<ExternalMessage> Receive(
            CancellationToken token
            , Func<InComingEventEntity, CancellationToken, Task<bool>> saveEntity
            , string queueName = "");

        Task ReceiveAck(string ackQueue,
            Func<ExternalMessage, Task<bool>> messageProcessor,
            CancellationToken token);
    }
}
