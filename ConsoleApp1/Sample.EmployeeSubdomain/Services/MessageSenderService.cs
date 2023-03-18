using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.DatabaseContext;
using Sample.EmployeeSubdomain.Interfaces;
using Sample.EmployeeSubdomain.Services.Interfaces;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg.Providers.Interfaces;
using System;
using System.Collections.Concurrent;
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
            //_ = Task.Run(()=> GenerateEmployee(token), token).ConfigureAwait(false);
            while (!token.IsCancellationRequested)
            {
                var externalMsgs = await _outgoingMessageProvider.GetMessages(token, (outgoingEvent => !outgoingEvent.IsDeleted && !outgoingEvent.IsSent)).ConfigureAwait(false);
                var sentMsgs = new ConcurrentBag<ExternalMessage>();
                var failMsgs = new ConcurrentBag<(ExternalMessage msg, MessageHandlingReason.SendFailedReason? reason, Exception exception)>();
                try
                {
                    await Parallel.ForEachAsync(externalMsgs, async (msg, token) =>
                            {
                                await _sender.Send(token, msg,
                                    msgSent =>
                                    {
                                        sentMsgs.Add(msg);
                                    },
                                    (msg, failReason, exception) =>
                                    {
                                        var failMsg = (msg, failReason, exception);
                                        failMsgs.Add(failMsg);
                                    }).ConfigureAwait(false);
                            });
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical, "An error ocurred processing messages to send");
                    await Task.Delay(5000, token).ConfigureAwait(false);
                }

                var sentMsgResult = await _outgoingMessageProvider.UpdateSentMessages(sentMsgs, token, null).ConfigureAwait(false);
                if (sentMsgs.Count != sentMsgResult) 
                {
                    _logger.LogDebug("Request to update sent message count {sentMsgs.Count} does not match result {sentMsgResult}", sentMsgs.Count, sentMsgResult);
                }

                await Task.Delay(1000, token).ConfigureAwait(false);
            }
            return true;
        }

        private async Task OnErrorSavingSentMessages(ExternalMessage message, CancellationToken token) 
        {

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
