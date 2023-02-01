using Sample.EmployeeSubdomain.Employee.Messages;
using Sample.Sdk.Core;

namespace TestProject1
{
    [TestClass]
    public class StaticBasedObjectTest
    {
        [TestMethod]
        public void WhenAregistrarSendNotfication()
        {
            var wasCalled = false;
            StaticBaseObject<EmployeeAdded>.Register(typeof(EmployeeAdded), msg => 
            {
                wasCalled = true;
                return true;
            });
            StaticBaseObject<EmployeeAdded>.Notify(new EmployeeAdded());
            Assert.IsTrue(wasCalled);
        }
    }
}