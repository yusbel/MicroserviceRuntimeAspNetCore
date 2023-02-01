using Sample.Sdk.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Payroll.Messages.Sent
{
    public class PayRollAdded : IMessage
    {
        private string _serializationType;

        public string PayRollIdentifier { get; set; }
        public string SerializationType { get => _serializationType; set => _serializationType = value; }
    }
}
