namespace Sample.Messaging.WebHooks
{
    public interface IWebHookMessageSubscriber
    {
        bool TryGetInMemmoryMessage(string senderKey, out IInMemmoryMessage<string> inMemmoryMessage);
        bool Subscribe(string senderKey);
        bool AddMessage(string msgKey, string message);
    }
}