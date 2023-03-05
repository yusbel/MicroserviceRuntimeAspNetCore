using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;

namespace Sample.Sdk.Msg.Providers
{
    public interface IOutgoingMessageProvider
    {
        Task<IEnumerable<ExternalMessage>> GetMessages(CancellationToken cancellationToken,
            Func<OutgoingEventEntity, bool> condition);
        Task<int> UpdateSentMessages(IEnumerable<ExternalMessage> sentMsgs,
            CancellationToken cancellationToken,
            Action<ExternalMessage, Exception> failSend);
        Task<int> UpdateSentMessages(IEnumerable<string> sentMsgs,
            CancellationToken cancellationToken,
            Func<OutgoingEventEntity, OutgoingEventEntity> updateEntity,
            Action<string, Exception> failSent);
    }
}