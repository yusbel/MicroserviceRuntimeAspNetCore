using Microsoft.Extensions.DependencyInjection;
using Sample.EmployeeSubdomain.DatabaseContext;
using Sample.Sdk.AspNetCore.Middleware;
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

        public MessageProcessAcknowledgement(IServiceScopeFactory serviceScopeFactory) 
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        public async Task<bool> Process(MessageProcessedAcknowledgement messageProcessedAcknowledgement)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeContext>();
            var externalMessage = System.Text.Json.JsonSerializer.Deserialize<ExternalMessage>(Convert.FromBase64String(messageProcessedAcknowledgement.EncryptedExternalMessage));
            var encryptedMsgWithMetadata = System.Text.Json.JsonSerializer.Deserialize<EncryptedMessageMetadata>(externalMessage.Content);
            var message = dbContext.ExternalEvents.FirstOrDefault(msg => msg.MessageKey == encryptedMsgWithMetadata.Key);
            if(message != null) 
            {
                message.WasAcknowledge = true;
                await dbContext.SaveChangesAsync();
            }
            return true;
        }
    }
}
