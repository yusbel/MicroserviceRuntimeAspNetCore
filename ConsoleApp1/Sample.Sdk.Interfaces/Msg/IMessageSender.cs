using Sample.Sdk.Data.Msg;
using static Sample.Sdk.Data.Enums.Enums;

namespace Sample.Sdk.Interface.Msg
{
    public interface IMessageSender
    {
        Task<(bool WasSent, SendFailedReason Reason)>
            SendMessage(CancellationToken token,
                    ExternalMessage msg,
                    Action<ExternalMessage> onSent,
                    Action<ExternalMessage, SendFailedReason?, Exception?> onError);

        Task<bool> SendMessages(Func<ExternalMessage, string> getQueue,
            IEnumerable<ExternalMessage> messages,
            Action<IEnumerable<ExternalMessage>> onSent,
            Action<IEnumerable<ExternalMessage>, Exception> onError,
            CancellationToken token);
    }
}
