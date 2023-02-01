using Sample.Messaging;
using Sample.Sdk.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestClass]
    public class InMemmoryMessageTest
    {
        private class Msg : IMessage
        {
            private string _serializationType;
            public string SerializationType { get => "Test"; set => _serializationType = value; }

            public string Name { get; init; }
        }
        [TestMethod]
        public void GivenExistingKeyThenUpdateMessages() 
        {
            var inMemmoryMessage = InMemmoryMessage<IMessage>.Create();
            inMemmoryMessage.Add("Test", new Msg() { Name = "Yusbel" });
            inMemmoryMessage.Add("Test", new Msg() { Name = "Test" });
            Assert.IsTrue(inMemmoryMessage.GetMessage("Test").Count() == 2);
        }

        [TestMethod]
        public void GivenExistingKeyIsNullThenThrowException() 
        {
            Assert.ThrowsException<ArgumentNullException>(() => InMemmoryMessage<IMessage>.Create().Add(null, new Msg() { Name = "Test" }));
        }
    }
}
