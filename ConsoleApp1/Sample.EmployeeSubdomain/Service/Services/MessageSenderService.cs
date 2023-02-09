using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Service.DatabaseContext;
using Sample.EmployeeSubdomain.Service.Services.Interfaces;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Service.Services
{
    public class MessageSenderService : IMessageSenderService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MessageSenderService> _logger;
        private readonly IMessageBusSender _sender;

        public MessageSenderService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<MessageSenderService> logger,
            IMessageBusSender sender)
        {
            _logger = logger;
            _sender = sender;
            _serviceScopeFactory = serviceScopeFactory;
        }
        /// <summary>
        /// Message will be marked delete once sent
        /// </summary>
        /// <param name="token"></param>
        /// <param name="delete"></param>
        /// <returns></returns>
        public async Task<bool> Send(CancellationToken token, bool delete = true)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext= scope.ServiceProvider.GetRequiredService<EmployeeContext>();
            //Create dbcontext for every batch of message
            while (!token.IsCancellationRequested)
            {
                var messages = await dbContext.ExternalEvents.Where(msg => msg.IsDeleted == false).ToListAsync();
                var sentMsgs = new List<IExternalMessage>();
                if (messages.Any())
                {
                    messages.RemoveAll(msg => msg == null);
                    var externalMessages = messages.ConvertAll<ExternalMessage>(msg =>
                    {
                        var etrnMsg = System.Text.Json.JsonSerializer.Deserialize<ExternalMessage>(msg.Body);
                        if (etrnMsg != null)
                        {
                            etrnMsg.CorrelationId = msg.Id.ToString();
                        }
                        return etrnMsg;
                    });
                    externalMessages?.RemoveAll(message => message == null);
                    if (externalMessages != null)
                    {
                        await _sender.Send("employeeadded", token, externalMessages, msgSend =>
                        {
                            sentMsgs.Add(msgSend);
                        });
                    }
                }
                if (sentMsgs.Any() && delete)
                {
                    sentMsgs.ForEach(async msg =>
                    {
                        var entity = messages.FirstOrDefault(item => item.Id.ToString() == msg.Key);
                        if (entity != null)
                        {
                            entity.IsDeleted = true;
                            await dbContext.SaveChangesAsync();
                        }
                    });
                }
                dbContext.ChangeTracker.Clear();
                await Task.Delay(1000);
            }
            return true;
        }
    }
}
