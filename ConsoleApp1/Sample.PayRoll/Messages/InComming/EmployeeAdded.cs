using Sample.Sdk.Core.Attributes;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Messages.InComming
{
    [MessageMetada("EmployeeAdded")]
    public class EmployeeAdded : ExternalMessage
    {
    }
}
