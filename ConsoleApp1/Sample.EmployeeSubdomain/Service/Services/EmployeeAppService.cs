using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Service.Interfaces;
using Sample.EmployeeSubdomain.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Service.Services
{
    public class EmployeeAppService : IEmployeeAppService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public EmployeeAppService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ILogger>();
            logger.LogInformation("Creating and saving employee");
            var employee = scope.ServiceProvider.GetRequiredService<IEmployee>();
            await employee.CreateAndSave("yusbel", "yusbel@gmail.com");
            while (!stoppingToken.IsCancellationRequested)
            {
                //in progress
                await Task.Delay(1000);
            }
        }
    }
}
