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
        public string NonceKey { get;init; } = string.Empty;
        public string NonceValue { get; init; } = string.Empty; 
        public string DoubleNonceKey { get; init; } = string.Empty;
        public string DoubleNonceValue { get;init; } = string.Empty;

        public string TagKey { get; init; } = string.Empty;
        public string TagValue { get; init; } = string.Empty;
        public string DoubleTagKey { get;init; } = string.Empty;
        public string DoubleTagValue { get;init; } = string.Empty;

        public string CypherPropertyNameKey { get; set; } = string.Empty;
        public string CypherPropertyValueKey { get; set; } = string.Empty;
        public string DoubleCypherPropertyNameKey { get; set; } = string.Empty;
        public string DoubleCypherPropertyKeyKey { get; set; } = string.Empty;

        /// <summary>
        /// for message integrity
        /// </summary>
        public string Signature { get; set; } = string.Empty;
        /// <summary>
        /// cypher text
        /// </summary>
        public string CypherContentKey { get; init; } = string.Empty;
        public string CypherContentValue { get; init; } = string.Empty;

    }
}
