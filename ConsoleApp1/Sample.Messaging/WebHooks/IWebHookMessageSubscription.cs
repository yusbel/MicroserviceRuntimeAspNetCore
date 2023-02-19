using Sample.Sdk.InMemory;

namespace Sample.Messaging.WebHooks
{
    public interface IWebHookMessageSubscription
    {
        bool TryGetInMemmoryMessage(string senderKey, out IInMemoryMessageBus<string> inMemmoryMessage);
        bool Subscribe(string senderKey);
        bool AddMessage(string msgKey, string message);
    }
}