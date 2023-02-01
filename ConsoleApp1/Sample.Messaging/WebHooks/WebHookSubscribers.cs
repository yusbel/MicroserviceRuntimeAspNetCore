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
    public class WebHookSubscribers : IWebHookSubscribers
    {
        private BlockingCollection<WebHookSubscriber> webHookSubscribers = new BlockingCollection<WebHookSubscriber>();

        public WebHookSubscriber Add(string senderKey, string messageKey, string webHookUrl)
        {
            if (webHookSubscribers.ToList().Any(webHook => webHook.MessageKey == messageKey && webHook.SenderKey == senderKey))
            {
                return webHookSubscribers.ToList().FirstOrDefault(item=> item.MessageKey == messageKey && item.SenderKey == senderKey);
            }
            var webHookSubs = new WebHookSubscriber()
            {
                SubscriptionKey = Guid.NewGuid().ToString(),
                SenderKey = senderKey,
                MessageKey = messageKey,
                WebHookUrl = webHookUrl
            };
            webHookSubscribers.Add(webHookSubs);
            return webHookSubs;
        }

        public IEnumerable<WebHookSubscriber> GetWebHookByMessageKey(string messageKey) => webHookSubscribers.ToList().Where(wh => wh.MessageKey == messageKey).ToList();

        public IEnumerable<WebHookSubscriber> GetWebHookBySenderKey(string senderKey) => webHookSubscribers.ToList().Where(item=> item.SenderKey == senderKey).ToList();

        public bool AnySubscription(string senderKey, string messageKey) 
        {
            return webHookSubscribers.Any(webHook => webHook.MessageKey == messageKey && webHook.SenderKey == senderKey);
        }
        public IEnumerable<WebHookSubscriber> GetWebHooks()
        {
            return webHookSubscribers.ToList();
        }


    }
}
