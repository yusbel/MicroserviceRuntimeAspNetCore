using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.DatabaseContext;
using Sample.EmployeeSubdomain.Interfaces;
using Sample.EmployeeSubdomain.Services.Interfaces;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg.Providers.Interfaces;
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
        private readonly IMessageSender _sender;
        private readonly IOutgoingMessageProvider _outgoingMessageProvider;

        public MessageSenderService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<MessageSenderService> logger,
            IMessageSender sender,
            IOutgoingMessageProvider outgoingMessageProvider)
        {
            _logger = logger;
            _sender = sender;
            _outgoingMessageProvider = outgoingMessageProvider;
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
        public async Task<bool> Send(CancellationToken token)
        {
            _ = Task.Run(()=> GenerateEmployee(token), token).ConfigureAwait(false);
            while (!token.IsCancellationRequested)
            {
                var externalMsgs = await _outgoingMessageProvider.GetMessages(token, null).ConfigureAwait(false);
                var sentMsgs = new List<ExternalMessage>();
                await _sender.Send(token, null, msgSent=>
                {
                    if (!sentMsgs.Any(msg=> msg.EntityId == msgSent.EntityId)) 
                    {
                        sentMsgs.Add(msgSent);
                    }
                }, null).ConfigureAwait(false);
                var sentMsgResult = await _outgoingMessageProvider.UpdateSentMessages(sentMsgs, token, null).ConfigureAwait(false);
                if (sentMsgs.Count != sentMsgResult) 
                {
                    _logger.LogDebug("Request to update sent message count {sentMsgs.Count} does not match result {sentMsgResult}", sentMsgs.Count, sentMsgResult);
                }
                await Task.Delay(1000, token).ConfigureAwait(false);
            }
            return true;
        }

        private async Task GenerateEmployee(CancellationToken token) 
        {
            var employee = _serviceScopeFactory.CreateAsyncScope().ServiceProvider.GetRequiredService<IEmployee>();
            int counter = 0;
            await employee.CreateAndSave("Yusbel", "Garcia Diaz", token);
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
