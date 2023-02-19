using Microsoft.Extensions.Hosting;
using Sample.PayRoll.Messages.InComming;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Services
{
    public class EmployeeAddedHostedService : IHostedService
    {
        private static object _locker = new object();
        private readonly IEmployeeAddedService _serviceEmployeeAdded;

        public EmployeeAddedHostedService(IEmployeeAddedService employeeAddedService)
        {
            _serviceEmployeeAdded = employeeAddedService;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() => 
            {
                lock(_locker) 
                {
                    _serviceEmployeeAdded.Process(cancellationToken);
                }
                
            });
            //var excecutingTask = _serviceEmployeeAdded.Process(cancellationToken); 
            //if(excecutingTask.IsCompleted) 
            //{
            //    return excecutingTask;
            //}
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
