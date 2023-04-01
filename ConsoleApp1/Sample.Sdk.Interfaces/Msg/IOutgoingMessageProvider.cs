using Sample.Sdk.Data.Entities;
using Sample.Sdk.Data.Msg;
using System.Linq.Expressions;

namespace Sample.Sdk.Interface.Msg
{
    public interface IOutgoingMessageProvider
    {
        Task<IEnumerable<ExternalMessage>> GetMessages(CancellationToken cancellationToken,
            Expression<Func<OutgoingEventEntity, bool>> condition);
        Task<int> UpdateSentMessages(IEnumerable<ExternalMessage> sentMsgs,
            CancellationToken cancellationToken,
            Action<ExternalMessage, Exception> failSend);
        Task<int> UpdateSentMessages(IEnumerable<string> sentMsgs,
            CancellationToken cancellationToken,
            Func<OutgoingEventEntity, OutgoingEventEntity> updateEntity,
            Action<string, Exception> failSent);
    }
}