using Sample.Messaging.WebHooks;

namespace Sample.Messaging.Test
{
    [TestClass]
    public class WebHookSubscribersTest
    {
        [TestMethod]
        public void WhenNewSenderKeyAndMessageKeyThenAddSubscriber()
        {
            var webHookSubscriber = new WebHookSubscription();
            webHookSubscriber.Add("EmployeeSubdomain", "PayRollAdded", "https://localhost");
            Assert.IsTrue(webHookSubscriber.GetWebHookBySubscriberKey("EmployeeSubdomain").ToList().Count == 1);
        }
    }
}