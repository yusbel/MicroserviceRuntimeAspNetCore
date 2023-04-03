using Sample.Sdk.Data.Entities;
using Sample.Sdk.Data.Msg;

namespace Sample.Sdk.Interface.Msg
{
    public interface IMessageReceiver
    {
        public Task<ExternalMessage> Receive(
            CancellationToken token
            , Func<InComingEventEntity, CancellationToken, Task<bool>> saveEntity
            , string queueName = "");

        Task ReceiveMessages(string queueName,
            Func<ExternalMessage, Task<bool>> messageProcessor,
            CancellationToken token);
    }
}
