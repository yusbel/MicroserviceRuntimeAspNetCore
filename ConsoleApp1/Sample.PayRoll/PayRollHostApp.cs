using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.PayRoll.WebHook;
using Sample.Sdk.Msg;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll
{
    public class PayRollHostApp : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PayRollHostApp(IServiceScopeFactory serviceScopeFactory) =>
            _serviceScopeFactory = serviceScopeFactory;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scope = _serviceScopeFactory.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<PayRollHostApp>();
            logger.LogInformation("Starting pay roll hosted service");
            var rnd = new Random();

            while (!stoppingToken.IsCancellationRequested)
            {
                //await RegisterNotifier.WebHook(new PayRollData() { EmployeeKey = Guid.NewGuid().ToString(), Salary = rnd.Next(1000) });
                await Task.Delay(10000);
            }

        }
    }
}
