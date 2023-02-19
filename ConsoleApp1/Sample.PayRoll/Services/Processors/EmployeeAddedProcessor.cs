using Microsoft.Extensions.DependencyInjection;
using Sample.PayRoll.Interfaces;
using Sample.PayRoll.Services.Processors.Converter;
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
    internal class EmployeeAddedProcessor : IMessageProcessor<EmployeeDto>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMessageConverter<EmployeeDto> _employeeConverter;

        public EmployeeAddedProcessor(
            IServiceScopeFactory serviceScopeFactory
            , IMessageConverter<EmployeeDto> employeeConverter) 
        {
            _serviceScopeFactory = serviceScopeFactory;
            _employeeConverter = employeeConverter;
        }
        public async Task<EmployeeDto> Process(CancellationToken token, ExternalMessage message)
        {
            var employee = _employeeConverter.Convert(message);
            var rnd = new Random();
            var salary = rnd.Next(100, 1000);
            using var scope = _serviceScopeFactory.CreateScope();
            var payRoll = scope.ServiceProvider.GetRequiredService<IPayRoll>();
            await payRoll.CreatePayRoll(employee.Id, salary, true);
            return employee;
        }
    }
}
