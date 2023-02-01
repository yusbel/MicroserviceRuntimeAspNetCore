using Sample.Sdk.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Employee.Messages
{
    public class EmployeeAdded : IMessage
    {
        private string _serializationType;
        public string EmployeeIdentifier { get; set; }
        public string SerializationType { get => _serializationType; set => _serializationType = value; }
    }
}
