﻿using Sample.Sdk.Core.Attributes;
using Sample.Sdk.Data.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Messages.InComming
{
    [MessageMetada("EmployeeUpdated", decryptScope:"")]
    public class EmployeeUpdated : ExternalMessage
    {
    }
}
