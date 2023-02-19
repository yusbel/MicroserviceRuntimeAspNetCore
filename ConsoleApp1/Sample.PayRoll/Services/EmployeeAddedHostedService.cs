using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IEmployeeAddedService _employeeAddedService;

        public EmployeeAddedHostedService(
            IServiceScopeFactory serviceScopeFactory
            , IEmployeeAddedService employeeAddedService)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _employeeAddedService = employeeAddedService;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(() => _employeeAddedService.Process(cancellationToken));
            if(task.IsCompleted) 
            {
                return task;
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
