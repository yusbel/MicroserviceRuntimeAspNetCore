using Sample.Sdk.Data.Entities;

namespace Sample.Sdk.Data.Msg
{
    public class CorruptedMessage : Entity
    {
        public string OriginalMessageKey { get; set; } = string.Empty;
        public string EncryptedContent { get; set; } = string.Empty;

    }
}
