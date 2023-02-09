using Sample.Sdk.Msg.Data;
using Sample.Sdk.Services;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Services.Processors
{
    public class EmployeeAddedProcessor : IMessageProcessor
    {
        public Task<bool> Process(CancellationToken token, ExternalMessage message)
        {
            return Task.FromResult(true);
        }
    }
}
