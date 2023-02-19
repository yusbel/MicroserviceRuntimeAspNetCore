using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.PayRoll.DatabaseContext;
using Sample.PayRoll.Interfaces;
using Sample.PayRoll.Messages.InComming;
using Sample.PayRoll.Services.Processors.Converter;
using Sample.Sdk.EntityModel;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMessageProcessor<EmployeeDto> _messageProcessor;
        private readonly IMessageConverter<EmployeeDto> _employeeAddedConverter;
        private readonly ILogger<EmployeeAddedService> _logger;

        public EmployeeAddedService(
            IMessageBusReceiver<EmployeeAdded> serviceEmpAdded
            , IServiceScopeFactory serviceScopeFactory
            , IMessageProcessor<EmployeeDto> messageProcessor
            , IMessageConverter<EmployeeDto> employeeAddedConverter
            , ILoggerFactory loggerFactory)
        {
            _serviceEmpAdded = serviceEmpAdded;
            _serviceScopeFactory = serviceScopeFactory;
            _messageProcessor = messageProcessor;
            _employeeAddedConverter = employeeAddedConverter;
            _logger = loggerFactory.CreateLogger<EmployeeAddedService>();
        }
        public async Task<bool> Process(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _serviceEmpAdded.Receive(token, async (inComingEvent) =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<PayRollContext>();
                        dbContext.Add(inComingEvent);
                        await dbContext.SaveChangesAsync();
                        return true;
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical("Exception occurred when saving entity {}", e);
                    }
                    return false;
                }, "EmployeeAdded");

                //Read from incoming event table
                await _serviceEmpAdded.Process(
                    getInComingEvents: async () =>
                                {
                                    try
                                    {
                                        using var scope = _serviceScopeFactory.CreateScope();
                                        var dbContext = scope.ServiceProvider.GetRequiredService<PayRollContext>();
                                        return await dbContext.InComingEvents
                                        .Where(e => !e.IsDeleted && !e.WasAcknowledge && !e.WasProcessed)
                                        .ToListAsync();
                                    }
                                    catch (Exception e)
                                    {
                                        _logger.LogCritical("An error occurred when reading incomming events {}", e);
                                        return Enumerable.Empty<InComingEventEntity>();
                                    }
                                },
                    processDeryptedInComingMessage: async (decryptExtMsg) => 
                                {
                                    try
                                    {
                                        using var scope = _serviceScopeFactory.CreateScope();
                                        var payRoll = scope.ServiceProvider.GetRequiredService<IPayRoll>();
                                        var employeeAdded = _employeeAddedConverter.Convert(decryptExtMsg);
                                        var rnd = new Random();
                                        var salary = rnd.Next(100, 1000);
                                        await payRoll.CreatePayRoll(employeeAdded.Id, salary, false);
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        _logger.LogCritical("An error occurrend processing incoming event {}", e);
                                        return false;
                                    }
                                },
                    updateEntity: async (inComingEvent) => 
                                {
                                    try
                                    {
                                        using var scope = _serviceScopeFactory.CreateScope();
                                        var dbContext = scope.ServiceProvider.GetRequiredService<PayRollContext>();
                                        inComingEvent.WasAcknowledge = true;
                                        inComingEvent.WasProcessed = true;
                                        dbContext.Entry(inComingEvent).State = EntityState.Modified;
                                        await dbContext.SaveChangesAsync();
                                        return true;
                                    }
                                    catch (Exception e)
                                    {
                                        _logger.LogCritical("An error occurred when updating incoming event to was processed and acknoeledge {}", e);
                                        return false;
                                    }
                                }, 
                    token: token);

                await Task.Delay(20000);
            }
            return true;
        }
    }
}
