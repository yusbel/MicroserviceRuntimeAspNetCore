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
    public class WebHookMessageSubscriber : IWebHookMessageSubscriber
    {
        private readonly ConcurrentDictionary<string, InMemmoryMessage<string>> messageSubscribers = new ConcurrentDictionary<string, InMemmoryMessage<string>>();
        private IWebHookSubscribers _webHookSubscribers;

        public WebHookMessageSubscriber(IWebHookSubscribers webHookSubscribers) => (_webHookSubscribers) = (webHookSubscribers);

        /// <summary>
        /// Create subscribe message if it does not exist
        /// </summary>
        /// <param name="senderKey"></param>
        /// <returns></returns>
        public bool Subscribe(string senderKey)
        {
            if (messageSubscribers.ContainsKey(senderKey))
            {
                return true;
            }
            //if sender is not suscribed then do not create subscribermessage
            if (_webHookSubscribers.GetWebHookBySenderKey(senderKey) == null) 
            {
                return false;
            }
            return messageSubscribers.TryAdd(senderKey, new InMemmoryMessage<string>());
        }
        /// <summary>
        /// Return message for each subscriber
        /// </summary>
        /// <param name="senderKey"></param>
        /// <returns></returns>
        public bool TryGetInMemmoryMessage(string senderKey, out IInMemmoryMessage<string> inMemmoryMessage)
        {
            inMemmoryMessage = null;
            if (!messageSubscribers.ContainsKey(senderKey))
            {
                if (!Subscribe(senderKey)) 
                {
                    return false;
                }
            }
            inMemmoryMessage = messageSubscribers[senderKey];
            return true;
        }

        /// <summary>
        /// Add message into subscribers inmemmory messages; if subscribed.
        /// Select all subscribermessages for this message key and add to the message subcriber
        /// </summary>
        /// <param name="messageKey"></param>
        /// <returns></returns>
        public bool AddMessage(string msgKey, string message) 
        {
            //select subscriber for this type of message
            var subscribers = _webHookSubscribers.GetWebHookByMessageKey(msgKey).Select(item=>item.SenderKey).ToList();
            if (!subscribers.Any()) 
            {
                return false;
            }
            //select subscribermessages for each subscriber and add the message
            messageSubscribers.ToList().Where(item => subscribers.Exists(e => e == item.Key)).ToList().ForEach(subscriber => 
            {
                subscriber.Value.Add(msgKey, message);
            });
            return true;
        }

    }
}
