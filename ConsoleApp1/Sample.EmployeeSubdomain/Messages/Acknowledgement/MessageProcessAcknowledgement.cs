using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.DatabaseContext;
using Sample.Sdk.AspNetCore.Middleware;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Messages.Acknowledgement
{
    public class MessageProcessAcknowledgement : IProcessAcknowledgement
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MessageProcessAcknowledgement> _logger;

        public MessageProcessAcknowledgement(
            IServiceScopeFactory serviceScopeFactory
            , ILogger<MessageProcessAcknowledgement> logger) 
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }
        public async Task<(bool, AcknowledgementResponseType)> Process(MessageProcessedAcknowledgement messageProcessedAcknowledgement)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            EmployeeContext dbContext;
            try
            {
                dbContext = scope.ServiceProvider.GetRequiredService<EmployeeContext>();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when retriving employee context");
                return (false, AcknowledgementResponseType.RetrieveDbContextFail);
            }
            ExternalMessage? externalMessage; 
            try
            {
                externalMessage = System.Text.Json.JsonSerializer.Deserialize<ExternalMessage>(Convert.FromBase64String(messageProcessedAcknowledgement.EncryptedExternalMessage));
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when deserializing to external message");
                return (false, AcknowledgementResponseType.DeserializationFail);
            }
            EncryptedMessageMetadata? encryptedMsgWithMetadata;
            try
            {
                encryptedMsgWithMetadata = System.Text.Json.JsonSerializer.Deserialize<EncryptedMessageMetadata>(externalMessage.Content);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when deserializing to encrypted metadata");
                return (false, AcknowledgementResponseType.DeserializationFail);
            }
            if (encryptedMsgWithMetadata == null || string.IsNullOrEmpty(encryptedMsgWithMetadata.Key))
            {
                return (false, AcknowledgementResponseType.DeserializationFail);
            }
            var message = dbContext.ExternalEvents.FirstOrDefault(msg => msg.MessageKey == encryptedMsgWithMetadata.Key);
            if(message != null) 
            {
                try
                {
                    message.WasAcknowledge = true;
                    await dbContext.SaveChangesAsync();
                    return (true, AcknowledgementResponseType.None);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "An error ocurred when saving to database");
                    return (false, AcknowledgementResponseType.SavingToDatabaseFail);
                }
            }
            return (false, AcknowledgementResponseType.NoAcknowledgeInDatabase);
        }
    }
}
