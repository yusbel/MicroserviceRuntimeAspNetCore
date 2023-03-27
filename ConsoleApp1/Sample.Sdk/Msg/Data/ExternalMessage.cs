using Microsoft.Graph.Models;
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
    public class ExternalMessage : InTransitData, IMessageIdentifier
    {
        private string _type = string.Empty;
        public string Type 
        {
            get 
            {
                if(_type == string.Empty) 
                {
                    return GetType().AssemblyQualifiedName!;
                }
                return _type;
            }
            set { _type = value; }
        }
        public string Content { get; init; } = string.Empty;
        /// <summary>
        /// Database entity unique identifier
        /// </summary>
        public string EntityId { get; set; } = string.Empty;
        /// <summary>
        /// Use the entity id as correlation id, the correlation id is used to map to the message hub schema
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// Message unique identifier used to track duplicate
        /// </summary>
        public string Id 
        {
            get;set;
        } = string.Empty;

        public string AckQueueName { get; set; } = string.Empty;
    }
}
