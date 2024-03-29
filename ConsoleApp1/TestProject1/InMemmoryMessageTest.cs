﻿using Sample.Sdk.Data.Msg;
using Sample.Sdk.InMemory;
using Sample.Sdk.Msg.Interfaces;
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
        private class Msg : ExternalMessage
        {
            private string _serializationType;
            public string SerializationType { get => "Test"; set => _serializationType = value; }

            public string Name { get; init; }
            public string MsgKey { get; set; }
            public string Content { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public string Key { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public string CorrelationId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }
        [TestMethod]
        public void GivenExistingKeyThenUpdateMessages() 
        {
            var inMemmoryMessage = new InMemoryMessageBus<ExternalMessage>();
            inMemmoryMessage.Add("Test", new Msg() { Name = "Yusbel" });
            inMemmoryMessage.Add("Test", new Msg() { Name = "Test" });
            Assert.IsTrue(inMemmoryMessage.GetAndRemove("Test").Count() == 2);
        }

        [TestMethod]
        public void GivenExistingKeyIsNullThenThrowException() 
        {
            Assert.ThrowsException<ArgumentNullException>(() => (new InMemoryMessageBus<ExternalMessage>()).Add(null, new Msg() { Name = "Test" }));
        }
    }
}
