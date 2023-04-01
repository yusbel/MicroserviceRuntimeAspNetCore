using Microsoft.Azure.Amqp.Framing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.PayRoll.DatabaseContext;
using Sample.PayRoll.Interfaces;
using Sample.PayRoll.Services.Processors.Converter;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Data.Msg;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Exceptions;
using Sample.Sdk.Interface.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Messages.InComming.Services
{
    /// <summary>
    /// T
    /// </summary>
    public class ComputeExternalMessage : IComputeExternalMessage
    {
        private readonly ILogger<ComputeExternalMessage> _logger;
        private readonly IPayRoll _payRoll;

        public ComputeExternalMessage(
            ILogger<ComputeExternalMessage> logger,
            IPayRoll payRoll)
        {
            _logger = logger;
            _payRoll = payRoll;
        }

        /// <summary>
        /// can be done by the sdk
        /// </summary>
        /// <param name="externalMessage"></param>
        /// <returns></returns>
        public Task<EmployeeAdded?> Convert(ExternalMessage externalMessage)
        {
            return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<EmployeeAdded>(externalMessage.Content));
        }

        public async Task<bool> ProcessExternalMessage(
            IServiceScope serviceScope,
            ExternalMessage externalMessage, 
            CancellationToken cancellationToken)
        {
            try
            {
                var payRoll = serviceScope.ServiceProvider.GetRequiredService<IPayRoll>();
                var employeeAdded = await Convert(externalMessage);
                var rnd = new Random();
                var salary = rnd.Next(100, 1000);
                await _payRoll.CreatePayRoll(employeeAdded.EntityId, salary, false, cancellationToken);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return false;
            }
        }

        public Task<bool> ProcessExternalMessage(List<KeyValuePair<string, string>> externalMessage, 
                                                    CancellationToken cancellationToken)
        {
            foreach(var key in externalMessage) 
            {
                _logger.LogInformation($"Key: {key} Value: {key.Value}");
            }
            return Task.FromResult(true);
        }

    }
}
