using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using System.Text.Json;

namespace Sample.Sdk.Msg.Providers
{
    /// <summary>
    /// Base class to convert outgoing event entity into external message, it would be used to encapsulate the abstract operations
    /// over different durable storage for outgoing event entity
    /// </summary>
    public class OutgoingMessageProvider
    {
        private readonly ILogger<OutgoingMessageProvider> _logger;

        public OutgoingMessageProvider(ILogger<OutgoingMessageProvider> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Convert outgoing event entity into external messages
        /// </summary>
        /// <param name="outgoingEvents">List of outgoing event entity</param>
        /// <returns>List<ExternalMessage></returns>
        protected List<ExternalMessage> ConvertToExternalMessage(List<OutgoingEventEntity> outgoingEvents)
        {
            List<ExternalMessage> externalMessages = new List<ExternalMessage>();
            foreach (var outgoingEvent in outgoingEvents)
            {
                try
                {
                    var encryptedMsg = JsonSerializer.Deserialize<EncryptedMessage>(outgoingEvent.Body);
                    externalMessages.Add(
                        new ExternalMessage
                        {
                            Id = outgoingEvent.Id,
                            Content = outgoingEvent.Body,
                            CorrelationId = encryptedMsg?.CorrelationId ?? string.Empty
                        });
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Fail to create external message");
                }
            }
            return externalMessages;
        }

        protected Task<IEnumerable<OutgoingEventEntity>?> ConvertToOutgoingEventEntity(IEnumerable<ExternalMessage> sentMsgs, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}