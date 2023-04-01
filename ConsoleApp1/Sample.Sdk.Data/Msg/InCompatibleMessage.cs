using Sample.Sdk.Data.Entities;

namespace Sample.Sdk.Data.Msg
{
    public class InCompatibleMessage : Entity
    {
        public string OriginalMessageKey { get; set; }
        public string OriginalType { get; set; }
        public string InCompatibleType { get; set; }
        public string EncryptedContent { get; set; }

    }
}
