using Sample.Messaging.WebHooks.Data;

namespace Sample.Messaging.WebHooks
{
    public interface IWebHookSubscription
    {
        bool TryAdd(string subscriberKey, string messageKey, string webHookUrl, out Data.WebHookSubscriber webHook);
        IEnumerable<Data.WebHookSubscriber> GetWebHooks();
        IEnumerable<Data.WebHookSubscriber> GetWebHookByMessageKey(string messageKey);
        IEnumerable<Data.WebHookSubscriber> GetWebHookBySubscriberKey(string subscriberKey);
        bool AnySubscription(string subscriberKey, string messageKey);
    }
}