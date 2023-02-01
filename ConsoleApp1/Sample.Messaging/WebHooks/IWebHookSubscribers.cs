using Sample.Messaging.WebHooks.Data;

namespace Sample.Messaging.WebHooks
{
    public interface IWebHookSubscribers
    {
        WebHookSubscriber Add(string senderKey, string subscriptionKey, string webHookUrl);
        IEnumerable<WebHookSubscriber> GetWebHooks();
        IEnumerable<WebHookSubscriber> GetWebHookByMessageKey(string messageKey);
        IEnumerable<WebHookSubscriber> GetWebHookBySenderKey(string senderKey);
        bool AnySubscription(string senderKey, string messageKey);
    }
}