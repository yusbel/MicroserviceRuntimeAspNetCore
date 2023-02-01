using Sample.Messaging.WebHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging.Publishers
{
    public class MessagePublisher : IMessagePublisher
    {
        private readonly IWebHookMessageSubscriber _webHookMsgSubs;
        private readonly IWebHookSubscribers _subscribers;
        private readonly IWebHookPublisher _webHookPublisher;

        public MessagePublisher(IWebHookMessageSubscriber webHookMessageSubscriber, IWebHookSubscribers subscribers, IWebHookPublisher webHookPublisher) =>
            (_webHookMsgSubs, _subscribers, _webHookPublisher) = (webHookMessageSubscriber, subscribers, webHookPublisher);
        public async Task Publish()
        {
            _subscribers.GetWebHooks().ToList().ForEach(sub =>
            {
                if (_webHookMsgSubs.TryGetInMemmoryMessage(sub.SenderKey, out var inMemmoryMsgs)) //get messages for this subscriber
                {
                    if (inMemmoryMsgs.TryGetMessage("PayRollAdded", out var msgs)) //get messages per type, a wildcard to be used
                    {
                        //invoke webhook
                        msgs.ToList().ForEach(async msg =>
                        {
                            await _webHookPublisher.Publish(sub.WebHookUrl, msg);       
                            await Task.Delay(TimeSpan.FromMilliseconds(100));
                        });
                    }
                }
            });
            await Task.CompletedTask;
        }
    }
}
