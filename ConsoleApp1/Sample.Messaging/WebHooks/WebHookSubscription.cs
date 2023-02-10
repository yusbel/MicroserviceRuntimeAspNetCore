using Sample.Messaging.WebHooks.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging.WebHooks
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public class WebHookSubscription : IWebHookSubscription
    {
        private BlockingCollection<Data.WebHookSubscriber> webHookSubscribers = new BlockingCollection<Data.WebHookSubscriber>();

        public bool TryAdd(string subscriberKey, string messageKey, string webHookUrl, out Data.WebHookSubscriber webHook)
        {
            webHook = webHookSubscribers.ToList().FirstOrDefault(item => item.MessageKey == messageKey && item.SubscriberKey == subscriberKey);
            if (webHook == null)
            {
                webHook = new Data.WebHookSubscriber()
                {
                    SubscriptionKey = Guid.NewGuid().ToString(),
                    SubscriberKey = subscriberKey,
                    MessageKey = messageKey,
                    WebHookUrl = webHookUrl
                };
                webHookSubscribers.Add(webHook);
                return true;
            }
            return false;
        }

        public IEnumerable<Data.WebHookSubscriber> GetWebHookByMessageKey(string messageKey) => webHookSubscribers.ToList().Where(wh => wh.MessageKey == messageKey).ToList();

        public IEnumerable<Data.WebHookSubscriber> GetWebHookBySubscriberKey(string subscriberKey) => webHookSubscribers.ToList().Where(item=> item.SubscriberKey == subscriberKey).ToList();

        public bool AnySubscription(string subscriberKey, string messageKey) 
        {
            return webHookSubscribers.Any(webHook => webHook.MessageKey == messageKey && webHook.SubscriberKey == subscriberKey);
        }
        public IEnumerable<Data.WebHookSubscriber> GetWebHooks()
        {
            return webHookSubscribers.ToList();
        }

        public void Remove(string subscriptionKey) 
        {
            var subscription = webHookSubscribers.ToList().FirstOrDefault(subscription => subscription.SubscriptionKey == subscriptionKey);
            if (subscription != null)
            {
                webHookSubscribers.ToList().Remove(subscription);
            }
        }

        public IEnumerable<Data.WebHookSubscriber> RemoveAll() 
        {
            if(webHookSubscribers.Count > 0) 
            {
                return webHookSubscribers.GetConsumingEnumerable();
            }
            return Enumerable.Empty<Data.WebHookSubscriber>();
        }
    }
}
