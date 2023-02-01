using Sample.Messaging.WebHooks;

namespace Sample.Messaging.Test
{
    [TestClass]
    public class WebHookSubscribersTest
    {
        [TestMethod]
        public void WhenNewSenderKeyAndMessageKeyThenAddSubscriber()
        {
            var webHookSubscriber = new WebHookSubscribers();
            webHookSubscriber.Add("EmployeeSubdomain", "PayRollAdded", "https://localhost");
            Assert.IsTrue(webHookSubscriber.GetWebHookBySenderKey("EmployeeSubdomain").ToList().Count == 1);
        }
    }
}