using Sample.Sdk.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Payroll.Messages.InComming
{
    public class EmployeeAdded : IMessage
    {
        private string _serializationType;
        public string EmployeeIentifier { get; set; }
        public string SerializationType { get => _serializationType; set => _serializationType = value; }
    }
}
