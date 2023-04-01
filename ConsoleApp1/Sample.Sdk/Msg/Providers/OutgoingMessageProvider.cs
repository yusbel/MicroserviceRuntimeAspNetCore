using Microsoft.Extensions.Logging;
using Sample.Sdk.Data.Entities;
using Sample.Sdk.Data.Msg;
using static Sample.Sdk.Core.Extensions.OutgoingEventEntityExtensions;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;

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
            var externalMessages = new List<ExternalMessage>();
            foreach (var outgoingEvent in outgoingEvents)
            {
                try
                {
                    var externalMsg = outgoingEvent.ConvertToExternalMessage();
                    if(externalMsg != null)
                        externalMessages.Add(externalMsg);
                }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
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