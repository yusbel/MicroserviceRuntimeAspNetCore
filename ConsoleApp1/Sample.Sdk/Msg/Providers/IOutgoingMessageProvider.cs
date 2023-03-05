using Sample.Sdk.Msg.Data;

namespace Sample.Sdk.Msg.Providers
{
    public interface IOutgoingMessageProvider
    {
        Task<IEnumerable<ExternalMessage>> GetMessages(CancellationToken cancellationToken);
        Task<int> UpdateSentMessages(IEnumerable<ExternalMessage> sentMsgs, CancellationToken cancellationToken);
    }
}