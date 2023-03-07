using Sample.Sdk.Core.Attributes;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Messages
{
    /// <summary>
    /// Event raised once an employee is created
    /// Attribute can dynamically be added using the app settings
    /// </summary>
    [MessageMetada(queueName: "employeeadded", decryptScope: "EmployeeAdded.Decrypt")]
    public class EmployeeAdded : ExternalMessage
    {
        public static EmployeeAdded CreateNotNullEvent() 
        { 
            return new EmployeeAdded(); 
        }
    }
}
