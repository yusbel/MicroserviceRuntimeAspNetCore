using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data
{
    /// <summary>
    /// Encrypted message
    /// </summary>
    public class EncryptedMessage : InTransitData
    {
        /// <summary>
        /// Map to the entity identifier that raised the event
        /// </summary>
        public string Key { get; init; } = string.Empty;
        /// <summary>
        /// used to link the message correlation id intransit with the entity and the message saved
        /// </summary>
        public string CorrelationId { get; init; } = string.Empty;
        /// <summary>
        /// point in time of creation
        /// </summary>
        public long CreatedOn { get; init; }
        
        public List<string> CypherPropertyNameKey { get; set; } = new List<string>();
        public List<string> CypherPropertyValueKey { get; set; } = new List<string>();

        /// <summary>
        /// for message integrity
        /// </summary>
        public string Signature { get; set; } = string.Empty;
       
    }
}
