using Sample.Sdk.Data.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Interface.Msg
{
    public interface ISendExternalMessage
    {
        void SendMessage(ExternalMessage externalMessage);
    }
}
