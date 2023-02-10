using Sample.Messaging.Bus;

namespace Sample.Messaging.WebHooks
{
    public interface IWebHookMessageSubscriber
    {
        bool TryGetInMemmoryMessage(string senderKey, out IInMemoryMessageBus<string> inMemmoryMessage);
        bool Subscribe(string senderKey);
        bool AddMessage(string msgKey, string message);
    }
}