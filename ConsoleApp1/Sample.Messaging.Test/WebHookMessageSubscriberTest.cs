using AutoFixture;
using Sample.Messaging.WebHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Messaging.Test
{
    [TestClass]
    public class WebHookMessageSubscriberTest
    {
        [TestMethod]
        public void WhenAddingMessageWithExistingSubscritionSenderKeyThenNewMessageAddedToSubscriptionSender() 
        {
            //var fixture = new Fixture();
            //var webHookSubscribers = fixture.Create<WebHookSubscription>();
            //webHookSubscribers.Add("EmployeeSubdomain", "PayRollAdded", "http://localhost");
            //fixture.Register<IWebHookSubscription>(() => webHookSubscribers);
            //var webHookMsgSubscriber = fixture.Create<WebHookMessageSubscription>();
            //webHookMsgSubscriber.Subscribe("EmployeeSubdomain");
            //fixture.Register<IWebHookMessageSubscription>(() => webHookMsgSubscriber);
            //webHookMsgSubscriber.AddMessage("PayRollAdded", "PayRoll added message");
            //if (webHookMsgSubscriber.TryGetInMemmoryMessage("PayRollAdded", out var inMemmoryMessage))
            //{
            //    Assert.IsNotNull(inMemmoryMessage);
            //}
            //if (webHookMsgSubscriber.TryGetInMemmoryMessage("EmployeeSubdomain", out var msgs))
            //{
            //    Assert.IsTrue(msgs.GetAndRemove("PayRollAdded").ToList().Count == 1);
            //}
        }

    }


}
