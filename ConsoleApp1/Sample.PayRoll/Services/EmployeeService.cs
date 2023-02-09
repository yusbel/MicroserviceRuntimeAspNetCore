using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sample.PayRoll.Messages.InComming;
using Sample.PayRoll.Services.Interfaces;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Services
{
    public class EmployeeService : ServiceRoot, IEmployeeService
    {
        private readonly IMessageBusReceiver<EmployeeAdded> _employeeAddedReceiver;
        private readonly IEnumerable<Func<Task<ExternalMessage>>> actions = new List<Func<Task<ExternalMessage>>>();
        public EmployeeService(
            IMessageBusReceiver<EmployeeAdded> employeeAddedReceiver,
            IEnumerable<IMessageProcessor> processors, 
            IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions) : base(processors, serviceBusInfoOptions)
        {
            _employeeAddedReceiver = employeeAddedReceiver;
        }

        protected override IEnumerable<Func<Task<ExternalMessage>>> GetMessageReceivers(CancellationToken token)
        {
            actions.ToList()
                .Add(async () =>
                {
                    return await _employeeAddedReceiver.Receive(token, async (message) =>
                    {
                        return await Task.FromResult(message);
                    });
                });
            return actions.ToList();
        }

    }
}
