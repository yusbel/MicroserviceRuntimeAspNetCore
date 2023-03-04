

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Learning.AspNetCoreMvc.Tests
{
    [TestClass]
    public class BasicTest : IClassFixture<WebApplicationFactory<Program>>
    {
        [TestMethod]
        public void TestMethod1()
        {
        }
    }
}