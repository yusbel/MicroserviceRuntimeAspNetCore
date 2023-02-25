using Microsoft.Azure.Amqp.Framing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.PayRoll.DatabaseContext;
using Sample.PayRoll.Interfaces;
using Sample.PayRoll.Services.Processors.Converter;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Services.Interfaces;
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
    public class EmployeeAddedMessageComputation : IMessageComputation<EmployeeAdded>
    {
        private readonly ILogger<EmployeeAddedMessageComputation> _logger;
        private readonly IPayRoll _payRoll;

        public EmployeeAddedMessageComputation(
            ILogger<EmployeeAddedMessageComputation> logger,
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

        public async Task<IEnumerable<InComingEventEntity>> GetInComingEventsAsync(
                        IServiceScope serviceScope,
                        Func<InComingEventEntity, bool> condition, 
                        CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<PayRollContext>();
                return await dbContext.InComingEvents
                                        .Where(e => condition(e))
                                        .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "An error occurred when reading incomming events");
                return Enumerable.Empty<InComingEventEntity>();
            }
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
                await _payRoll.CreatePayRoll(employeeAdded.Key, salary, false, cancellationToken);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "An error occurrend processing incoming event");
                return false;
            }
        }

        public async Task<bool> SaveInComingEventEntity(
            IServiceScope serviceScope, 
            InComingEventEntity eventEntity, 
            CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<PayRollContext>();
                dbContext.Add(eventEntity);
                await dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch(OperationCanceledException) { throw; }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "Exception occurred when saving entity");
                return false;
            }
        }

        public async Task<bool> UpdateInComingEventEntity(
            IServiceScope serviceScope,
            InComingEventEntity eventEntity, 
            CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<PayRollContext>();
                dbContext.Entry(eventEntity).State = EntityState.Modified;
                await dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch(OperationCanceledException) { throw; }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "An error occurred");
                return false;
            }
        }
    }
}
