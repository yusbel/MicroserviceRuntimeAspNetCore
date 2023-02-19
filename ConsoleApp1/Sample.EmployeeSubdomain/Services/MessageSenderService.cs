using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.DatabaseContext;
using Sample.EmployeeSubdomain.Interfaces;
using Sample.EmployeeSubdomain.Services.Interfaces;
using Sample.Sdk.Core;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Services
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

        public async Task<bool> Acknowledgement(ExternalMessage externalMsg, CancellationToken token)
        {

            return true;
        }

        /// <summary>
        /// Message will be marked delete once sent
        /// </summary>
        /// <param name="token"></param>
        /// <param name="delete"></param>
        /// <returns></returns>
        public async Task<bool> Send(CancellationToken token, bool delete = false)
        {
            Task.Run(()=> GenerateEmployee());
            using(var scope = _serviceScopeFactory.CreateScope())
            {
                using (var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeContext>())
                {
                    //Create dbcontext for every batch of message
                    while (!token.IsCancellationRequested)
                    {
                        var messages = await dbContext.ExternalEvents.Where(msg => msg.IsDeleted == false).AsNoTracking().ToListAsync();
                        var sentMsgs = new List<ExternalMessage>();
                        if (messages.Any())
                        {
                            messages.RemoveAll(msg => msg == null);
                            var externalMsgs = messages.Select(msg =>
                            {
                                var msgMetadata = System.Text.Json.JsonSerializer.Deserialize<EncryptedMessageMetadata>(msg.Body);
                                return new ExternalMessage()
                                {
                                    Key = msg.Id.ToString(),
                                    CorrelationId = msgMetadata?.CorrelationId ?? String.Empty,
                                    Content = msg.Body
                                };
                            }).ToList();
                            if (externalMsgs.Any())
                            {
                                await _sender.Send("employeeadded", token, externalMsgs, msgSend =>
                                {
                                    if (!sentMsgs.Any(msg=> msg.Key == msgSend.Key)) 
                                    {
                                        sentMsgs.Add(msgSend);
                                    }
                                });
                            }
                            if (sentMsgs.Any() && delete)
                            {
                                foreach (var msg in sentMsgs)
                                {
                                    var entity = await dbContext.ExternalEvents
                                                                .Where(msg => msg.IsDeleted == false)
                                                                .FirstOrDefaultAsync(item => item.Id == msg.Key);
                                    if (entity != null)
                                    {
                                        entity.IsDeleted = true;
                                        await dbContext.SaveChangesAsync();
                                    }
                                    
                                }
                            }
                        }
                        await Task.Delay(1000);
                    }
                }
                
            }
            return true;
        }

        private async Task GenerateEmployee() 
        {
            var employee = _serviceScopeFactory.CreateAsyncScope().ServiceProvider.GetRequiredService<IEmployee>();
            int counter = 0;
            await employee.CreateAndSave("Yusbel", "Garcia Diaz");
            return;
            //while (employee != null)
            //{
            //    await employee.CreateAndSave("Yusbel", "Garcia Diaz");
            //    counter++;
            //    if(counter == 10) 
            //    {
            //        await Task.Delay(10000);
            //        counter= 0;
            //    }
            //    await Task.Delay(1000);
            //}
        }
    }
}
