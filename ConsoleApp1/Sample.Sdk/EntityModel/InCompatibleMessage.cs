using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.EntityModel
{
    public class InCompatibleMessage : Entity, IMessageIdentifier
    {
        public string OriginalMessageKey { get; set; }
        public string OriginalType { get; set; }
        public string InCompatibleType { get; set; }
        public string EncryptedContent { get; set; }

    }
}
