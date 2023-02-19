using Sample.PayRoll.Messages.InComming;
using Sample.Sdk.Msg.Interfaces;
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

        public EmployeeAddedService(
            IMessageBusReceiver<EmployeeAdded> serviceEmpAdded)
        {
            _serviceEmpAdded = serviceEmpAdded;
        }
        public async Task<bool> Process(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _serviceEmpAdded.Receive(token, async (employee) =>
                {
                    await Task.Delay(1000);
                    return employee;
                }, "EmployeeAdded");
            }
            return true;
        }
    }
}
