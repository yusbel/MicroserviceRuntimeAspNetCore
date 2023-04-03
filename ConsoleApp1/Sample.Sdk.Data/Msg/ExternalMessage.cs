namespace Sample.Sdk.Data.Msg
{
    /// <summary>
    /// Message being save on durable storage to send to the serivice provider
    /// </summary>
    public class ExternalMessage : InTransitData
    {
        private string _type = string.Empty;
        public string Type
        {
            get
            {
                if (_type == string.Empty)
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

        public string AckQueueName { get; set; } = string.Empty;
    }
}
