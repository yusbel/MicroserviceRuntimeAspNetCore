
using Sample.EmployeeSubdomain.Messages;
using Sample.Sdk;

namespace TestProject1
{
    [TestClass]
    public class StaticBasedObjectTest
    {
        [TestMethod]
        public void WhenAregistrarSendNotfication()
        {
            var wasCalled = false;
            MessageNotifier<EmployeeAdded>.Register(typeof(EmployeeAdded), msg => 
            {
                wasCalled = true;
                return true;
            });
            MessageNotifier<EmployeeAdded>.Notify(new EmployeeAdded());
            Assert.IsTrue(wasCalled);
        }
    }
}