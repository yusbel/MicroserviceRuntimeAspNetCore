using Moq;
using Sample.Sdk.Core.Security.Providers.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Tests.Security
{
    [TestClass]
    internal class PointToPointChannelTests
    {
        private string _externalWellKnownEndpoint = "http://localhost/";

        [TestInitialize]
        public void Initialize() 
        {

        }

        [TestMethod]
        public void GivenValidCertificateThenCreateChannel() 
        {
            //var channel = new PointToPointChannel();
            //var identifier = Convert.ToBase64String(Encoding.UTF8.GetBytes(_externalWellKnownEndpoint));
            //var mockHttpClient = new Mock<HttpClient>();
            //mockHttpClient.Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
            //    .ReturnsAsync((HttpResponseMessage)null);

        }

    }
}
