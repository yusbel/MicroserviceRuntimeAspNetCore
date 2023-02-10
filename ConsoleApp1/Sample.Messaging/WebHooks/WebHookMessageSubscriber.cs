using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sample.Messaging.Bus;

namespace Sample.Messaging.WebHooks
{
    /// <summary>
    /// Singleton instance
    /// </summary>
    public class WebHookMessageSubscriber : IWebHookMessageSubscriber
    {
        private readonly ConcurrentDictionary<string, InMemoryMessageBus<string>> messageSubscribers = new ConcurrentDictionary<string, InMemoryMessageBus<string>>();
        private IWebHookSubscription _webHookSubscriber;

        public WebHookMessageSubscriber(IWebHookSubscription webHookSubscribers) => (_webHookSubscriber) = (webHookSubscribers);

        /// <summary>
        /// Create subscribe message if it does not exist
        /// </summary>
        /// <param name="subscriberKey"></param>
        /// <returns></returns>
        public bool Subscribe(string subscriberKey)
        {
            if (messageSubscribers.ContainsKey(subscriberKey))
            {
                return true;
            }
            //if subscriber is not suscribed for web hook no need to create a subscrition message
            if (_webHookSubscriber.GetWebHookBySubscriberKey(subscriberKey) == null) 
            {
                return false;
            }
            //creating subscriber message
            return messageSubscribers.TryAdd(subscriberKey, new InMemoryMessageBus<string>());
        }
        /// <summary>
        /// Return message for each subscriber
        /// </summary>
        /// <param name="subscriberKey"></param>
        /// <returns></returns>
        public bool TryGetInMemmoryMessage(string subscriberKey, out IInMemoryMessageBus<string> inMemmoryMessage)
        {
            inMemmoryMessage = null;
            if (!messageSubscribers.ContainsKey(subscriberKey) && !Subscribe(subscriberKey))
            {
                return false;
            }
            inMemmoryMessage = messageSubscribers[subscriberKey];
            return true;
        }

        /// <summary>
        /// Add message into subscribers inmemory messages; if subscribed.
        /// Select all subscribermessages for this message key and add to the message subcriber
        /// </summary>
        /// <param name="messageKey"></param>
        /// <returns></returns>
        public bool AddMessage(string msgKey, string message) 
        {
            //select subscriber for this type of message
            var subscribers = _webHookSubscriber.GetWebHookByMessageKey(msgKey)
                                                .Select(webHookSubscribers => webHookSubscribers.SubscriberKey)
                                                .ToList();
            if (!subscribers.Any()) 
            {
                return false;
            }
            //select subscribermessages for each subscriber and add the message
            messageSubscribers.ToList()
                .Where(subscribedList => subscribers.Exists(subscriberKey => subscriberKey == subscribedList.Key))
                .ToList()
                .ForEach(subscriber => 
                            {
                                subscriber.Value.Add(msgKey, message);
                            });
            return true;
        }

    }
}
