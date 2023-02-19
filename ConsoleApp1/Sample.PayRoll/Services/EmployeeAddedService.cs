using Sample.PayRoll.Messages.InComming;
using Sample.PayRoll.Services.Processors.Converter;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Services
{
    public class EmployeeAddedService : IEmployeeAddedService
    {
        private readonly IMessageBusReceiver<EmployeeAdded> _serviceEmpAdded;
        private readonly IMessageProcessor<EmployeeDto> _messageProcessor;

        public EmployeeAddedService(
            IMessageBusReceiver<EmployeeAdded> serviceEmpAdded
            , IMessageProcessor<EmployeeDto> messageProcessor)
        {
            _serviceEmpAdded = serviceEmpAdded;
            _messageProcessor = messageProcessor;
        }
        public async Task<bool> Process(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _serviceEmpAdded.Receive(token, async (employee) =>
                {
                    await _messageProcessor.Process(token, employee);
                    return employee;
                }, "EmployeeAdded");
            }
            return true;
        }
    }
}
