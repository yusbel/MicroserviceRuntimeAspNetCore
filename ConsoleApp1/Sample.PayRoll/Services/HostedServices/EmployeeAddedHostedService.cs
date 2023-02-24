using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.PayRoll.Messages.InComming;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.PayRoll.Services.HostedServices
{
    public class EmployeeAddedHostedService : MessageRealtimeHostedService<EmployeeAdded>
    {
        public EmployeeAddedHostedService(
            ILogger<MessageRealtimeHostedService<EmployeeAdded>> logger, 
            IHostApplicationLifetime hostAppLifetime) : base(
                logger, 
                hostAppLifetime)
        {
        }
    }
}
