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

        public EmployeeAddedHostedService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var serviceEmpAddedService = scope.ServiceProvider.GetService<IEmployeeAddedService>();
                serviceEmpAddedService.Process(cancellationToken);
            }

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
