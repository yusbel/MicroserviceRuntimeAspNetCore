using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data
{
    /// <summary>
    /// Message being save on durable storage to send to the serivice provider
    /// </summary>
    public class ExternalMessage : MessageMetaData, IMessageIdentifier
    {
        /// <summary>
        /// Database entity unique identifier
        /// </summary>
        public string EntityId { get; set; } = string.Empty;
        /// <summary>
        /// Use the entity id as correlation id, the correlation id is used to map to the message hub schema
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;
        /// <summary>
        /// Encrypted entity with security attributes
        /// </summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Message unique identifier used to track duplicate
        /// </summary>
        public string Id 
        {
            get;set;
        } = string.Empty;
    }
}
